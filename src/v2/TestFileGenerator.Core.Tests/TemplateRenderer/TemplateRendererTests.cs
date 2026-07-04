using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

namespace TestFileGenerator.Core.Tests.TemplateRenderer;

/// <summary>
/// Tests for TemplateRenderer template processing.
/// </summary>
[TestFixture]
public class TemplateRendererTests
{
    private Mock<IGeneralFileWriter> _mockFileWriter = null!;
    private Mock<IGeneratorCliOptions> _mockOptions = null!;
    private Mock<ILogger<UTMO.Text.FileGenerator.TemplateRenderer>> _mockLogger = null!;
    private IConfiguration _defaultConfiguration = null!;
    private UTMO.Text.FileGenerator.TemplateRenderer _renderer = null!;
    private string _testTemplateDir = null!;

    [SetUp]
    public void Setup()
    {
        _mockFileWriter = new Mock<IGeneralFileWriter>();
        _mockOptions = new Mock<IGeneratorCliOptions>();
        _mockLogger = new Mock<ILogger<UTMO.Text.FileGenerator.TemplateRenderer>>();
        _defaultConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        
        _testTemplateDir = Path.Combine(Path.GetTempPath(), $"TemplateTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testTemplateDir);
        
        _mockOptions.Setup(o => o.TemplatePath).Returns(_testTemplateDir);
        _mockOptions.Setup(o => o.OutputPath).Returns(Path.GetTempPath());
        _mockOptions.Setup(o => o.AllowOverwrite).Returns(false);
        
        _renderer = new UTMO.Text.FileGenerator.TemplateRenderer(_mockOptions.Object, _mockFileWriter.Object, _mockLogger.Object, _defaultConfiguration);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testTemplateDir))
        {
            Directory.Delete(_testTemplateDir, recursive: true);
        }
    }

    [Test]
    public async Task GenerateFile_WithMissingTemplate_ShouldThrowTemplateNotFoundException()
    {
        // Arrange
        var templateName = "nonexistent.liquid";
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        var act = async () => await _renderer.GenerateFile(templateName, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<TemplateNotFoundException>()
            .Where(ex => ex.TemplateName == "nonexistent.liquid");
    }

    [Test]
    public async Task GenerateFile_WithValidTemplate_ShouldRenderAndWriteFile()
    {
        // Arrange
        var templateName = "test.liquid";
        var templateContent = "Hello {{ name }}!";
        var templatePath = Path.Combine(_testTemplateDir, templateName);
        await File.WriteAllTextAsync(templatePath, templateContent);
        
        var outputFile = "output.txt";
        var context = new Dictionary<string, object> { { "name", "World" } };

        // Act
        await _renderer.GenerateFile(templateName, outputFile, context);

        // Assert
        _mockFileWriter.Verify(fw => fw.WriteFile(
            outputFile,
            "Hello World!",
            false), Times.Once);
    }

    [Test]
    public async Task GenerateFile_WhenAllowOverwriteIsTrue_ShouldPassOverwriteFlagToWriter()
    {
        // Arrange
        var templateName = "test.liquid";
        var templateContent = "Hello {{ name }}!";
        var templatePath = Path.Combine(_testTemplateDir, templateName);
        await File.WriteAllTextAsync(templatePath, templateContent);

        _mockOptions.Setup(o => o.AllowOverwrite).Returns(true);
        _renderer = new UTMO.Text.FileGenerator.TemplateRenderer(_mockOptions.Object, _mockFileWriter.Object, _mockLogger.Object, _defaultConfiguration);

        var outputFile = "output.txt";
        var context = new Dictionary<string, object> { { "name", "World" } };

        // Act
        await _renderer.GenerateFile(templateName, outputFile, context);

        // Assert
        _mockFileWriter.Verify(fw => fw.WriteFile(outputFile, "Hello World!", true), Times.Once);
    }

    [Test]
    public async Task GenerateFile_WithoutLiquidExtension_ShouldAddExtension()
    {
        // Arrange
        var templateName = "test"; // no .liquid extension
        var templateContent = "Content";
        var templatePath = Path.Combine(_testTemplateDir, "test.liquid");
        await File.WriteAllTextAsync(templatePath, templateContent);
        
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        await _renderer.GenerateFile(templateName, outputFile, context);

        // Assert - should find the template with .liquid extension added
        _mockFileWriter.Verify(fw => fw.WriteFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Test]
    public async Task GenerateFile_WithEmptyTemplateOutput_ShouldThrowNoGeneratedTextException()
    {
        // Arrange
        var templateName = "empty.liquid";
        var templateContent = ""; // empty template
        var templatePath = Path.Combine(_testTemplateDir, templateName);
        await File.WriteAllTextAsync(templatePath, templateContent);
        
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        var act = async () => await _renderer.GenerateFile(templateName, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<NoGeneratedTextException>();
    }

    [Test]
    public async Task GenerateFile_WithGlobalContext_ShouldMergeContexts()
    {
        // Arrange
        var templateName = "global.liquid";
        var templateContent = "Global: {{ global_var }}, Local: {{ local_var }}";
        var templatePath = Path.Combine(_testTemplateDir, templateName);
        await File.WriteAllTextAsync(templatePath, templateContent);
        
        var outputFile = "output.txt";
        
        // Add global context
        _renderer.AddToGlobalContext("global_var", "GlobalValue");
        
        var localContext = new Dictionary<string, object> { { "local_var", "LocalValue" } };

        // Act
        await _renderer.GenerateFile(templateName, outputFile, localContext);

        // Assert
        _mockFileWriter.Verify(fw => fw.WriteFile(
            outputFile,
            "Global: GlobalValue, Local: LocalValue",
            false), Times.Once);
    }

    [Test]
    public async Task AddToGlobalContext_MultipleCalls_ShouldAccumulateContext()
    {
        // Arrange
        var templateName = "multi.liquid";
        var templateContent = "A: {{ a }}, B: {{ b }}, C: {{ c }}";
        var templatePath = Path.Combine(_testTemplateDir, templateName);
        await File.WriteAllTextAsync(templatePath, templateContent);
        
        var outputFile = "output.txt";

        // Act - Add multiple items to global context
        _renderer.AddToGlobalContext("a", "ValueA");
        _renderer.AddToGlobalContext("b", "ValueB");
        _renderer.AddToGlobalContext(new Dictionary<string, object> { { "c", "ValueC" } });
        
        await _renderer.GenerateFile(templateName, outputFile, new Dictionary<string, object>());

        // Assert
        _mockFileWriter.Verify(fw => fw.WriteFile(
            outputFile,
            "A: ValueA, B: ValueB, C: ValueC",
            false), Times.Once);
    }

    [Test]
    public async Task GenerateFile_WithComplexTemplate_ShouldRenderCorrectly()
    {
        // Arrange
        var templateName = "complex.liquid";
        var templateContent = @"
{% for item in items %}
  - {{ item.name }}: {{ item.value }}
{% endfor %}
Total: {{ total }}";
        var templatePath = Path.Combine(_testTemplateDir, templateName);
        await File.WriteAllTextAsync(templatePath, templateContent);
        
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>
        {
            { "items", new List<Dictionary<string, object>>
                {
                    new() { { "name", "Item1" }, { "value", "10" } },
                    new() { { "name", "Item2" }, { "value", "20" } }
                }
            },
            { "total", "30" }
        };

        // Act
        await _renderer.GenerateFile(templateName, outputFile, context);

        // Assert
        _mockFileWriter.Verify(fw => fw.WriteFile(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("Item1: 10") && s.Contains("Item2: 20") && s.Contains("Total: 30")),
            false), Times.Once);
    }

    [Test]
    [CancelAfter(10_000)]
    public async Task GenerateFile_WithLongRunningTemplate_ShouldThrowOnTimeout()
    {
        // Arrange - a template with a large loop count designed to trigger timeout.
        // DotLiquid's native cooperative Timeout (set via RenderParameters.Timeout) now actually
        // stops the render thread rather than merely abandoning it on the ThreadPool, so a
        // generous-but-not-astronomical count is sufficient while keeping test CPU cost reasonable.
        const int largeLoopIterationCount = 100_000_000;
        var templateName = "slow.liquid";
        var templateContent = $"{{% for i in (1..{largeLoopIterationCount}) %}}{{{{ i }}}}{{% endfor %}}";
        var safeTemplateName = Path.GetFileName(templateName);
        if (Path.IsPathRooted(safeTemplateName))
            throw new ArgumentException($"Template name resolves to a rooted path after sanitization: {safeTemplateName}", nameof(templateName));
        var templatePath = Path.Combine(_testTemplateDir, safeTemplateName);
        await File.WriteAllTextAsync(templatePath, templateContent);

        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Configure a 1-second timeout via IConfiguration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TemplateRendering:TimeoutSeconds"] = "1",
                // Set a very large size limit so the test deterministically hits the timeout
                // path rather than the output-size limit (100M × ~8 bytes/number ≈ 800 MB > 10 MB default).
                ["TemplateRendering:MaxOutputSizeBytes"] = $"{1024 * 1024 * 1024}"
            })
            .Build();

        var renderer = new UTMO.Text.FileGenerator.TemplateRenderer(
            _mockOptions.Object, _mockFileWriter.Object, _mockLogger.Object, config);

        // Act
        var act = async () => await renderer.GenerateFile(templateName, outputFile, context);

        // Assert - should throw TemplateRenderingException due to timeout
        await act.Should().ThrowAsync<TemplateRenderingException>()
            .WithMessage("*timeout*");
    }

    [Test]
    public async Task GenerateFile_WithOutputExceedingMaxSize_ShouldThrowTemplateRenderingException()
    {
        // Arrange - a template that produces output exceeding the configured limit.
        // 100 iterations × 200 ASCII chars = 20,000 bytes — well above the 100-byte limit.
        const string paddingChar = "A";
        const int charsPerIteration = 200;
        var templateName = "large.liquid";
        // Build the template content directly so the test is readable
        var templateContent = "{% for i in (1..100) %}" + new string(paddingChar[0], charsPerIteration) + "{% endfor %}";
        var safeTemplateName = Path.GetFileName(templateName);
        if (Path.IsPathRooted(safeTemplateName))
            throw new ArgumentException($"Template name resolves to a rooted path after sanitization: {safeTemplateName}", nameof(templateName));
        var templatePath = Path.Combine(_testTemplateDir, safeTemplateName);
        await File.WriteAllTextAsync(templatePath, templateContent);

        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Configure a very small max output size (100 bytes) via IConfiguration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TemplateRendering:MaxOutputSizeBytes"] = "100"
            })
            .Build();

        var renderer = new UTMO.Text.FileGenerator.TemplateRenderer(
            _mockOptions.Object, _mockFileWriter.Object, _mockLogger.Object, config);

        // Act
        var act = async () => await renderer.GenerateFile(templateName, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<TemplateRenderingException>()
            .WithMessage("*exceeds maximum allowed size*");
    }

    #region Security Tests - Path Traversal Vulnerability (Issue #11)

    [Test]
    [TestCase("../../etc/passwd")]
    [TestCase("../../../etc/passwd")]
    public async Task GenerateFile_WithPathTraversalSequence_ShouldThrowInvalidTemplatePathException(string maliciousPath)
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        var act = async () => await _renderer.GenerateFile(maliciousPath, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<InvalidTemplatePathException>();
    }

    [Test]
    [Platform("Win")]
    [TestCase("..\\..\\..\\Windows\\System32\\drivers\\etc\\hosts")]
    [TestCase("..\\..\\appsettings.json")]
    public async Task GenerateFile_WithWindowsStylePathTraversalSequence_ShouldThrowInvalidTemplatePathException(string maliciousPath)
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        var act = async () => await _renderer.GenerateFile(maliciousPath, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<InvalidTemplatePathException>();
    }

    [Test]
    public async Task GenerateFile_WithAbsolutePathOnUnix_ShouldThrowInvalidTemplatePathException()
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();
        var absolutePath = "/etc/passwd";

        // Act
        var act = async () => await _renderer.GenerateFile(absolutePath, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<InvalidTemplatePathException>();
    }

    [Test]
    [Platform("Win")]
    public async Task GenerateFile_WithAbsolutePathOnWindows_ShouldThrowInvalidTemplatePathException()
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();
        var absolutePath = "C:\\Windows\\System32\\drivers\\etc\\hosts";

        // Act
        var act = async () => await _renderer.GenerateFile(absolutePath, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<InvalidTemplatePathException>();
    }

    [Test]
    public async Task GenerateFile_WithHomeDirectoryReference_ShouldThrowInvalidTemplatePathException()
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();
        var homePath = "~/.ssh/id_rsa";

        // Act
        var act = async () => await _renderer.GenerateFile(homePath, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<InvalidTemplatePathException>();
    }

    [Test]
    public async Task GenerateFile_WithEscapedPath_ShouldThrowInvalidTemplatePathException()
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();
        // Create a parent directory with a test file
        var parentDir = Path.GetDirectoryName(_testTemplateDir);
        var testFile = Path.Combine(parentDir!, "sensitive.txt");
        await File.WriteAllTextAsync(testFile, "sensitive data");

        try
        {
            var escapedPath = "../sensitive.txt";

            // Act
            var act = async () => await _renderer.GenerateFile(escapedPath, outputFile, context);

            // Assert
            await act.Should().ThrowAsync<InvalidTemplatePathException>();
        }
        finally
        {
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [Test]
    public async Task GenerateFile_WithValidRelativePath_ShouldSucceed()
    {
        // Arrange
        // Create a subdirectory with a template
        var subDir = Path.Combine(_testTemplateDir, "templates");
        Directory.CreateDirectory(subDir);
        
        var templateName = "templates/valid.liquid";
        var templateContent = "Valid content";
        var templatePath = Path.Combine(subDir, "valid.liquid");
        await File.WriteAllTextAsync(templatePath, templateContent);
        
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        await _renderer.GenerateFile(templateName, outputFile, context);

        // Assert
        _mockFileWriter.Verify(fw => fw.WriteFile(
            outputFile,
            templateContent,
            false), Times.Once);
    }

    [Test]
    public async Task GenerateFile_WithTemplatePathHavingTrailingSeparator_ShouldSucceed()
    {
        // Regression test: when TemplatePath ends with a directory separator,
        // the containment check in ValidateTemplatePath must not produce a double separator
        // that would cause valid relative template names to be incorrectly rejected.
        
        // Arrange - configure options with a trailing separator on TemplatePath
        var trailingSlashOptions = new Mock<IGeneratorCliOptions>();
        trailingSlashOptions.Setup(o => o.TemplatePath).Returns(_testTemplateDir + Path.DirectorySeparatorChar);
        trailingSlashOptions.Setup(o => o.OutputPath).Returns(Path.GetTempPath());

        var rendererWithTrailingSeparator = new UTMO.Text.FileGenerator.TemplateRenderer(
            trailingSlashOptions.Object, _mockFileWriter.Object, _mockLogger.Object);

        var templateContent = "Trailing separator content";
        var templateFile = Path.Combine(_testTemplateDir, "trailing.liquid");
        await File.WriteAllTextAsync(templateFile, templateContent);

        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        await rendererWithTrailingSeparator.GenerateFile("trailing", outputFile, context);

        // Assert
        _mockFileWriter.Verify(fw => fw.WriteFile(
            outputFile,
            templateContent,
            false), Times.Once);
    }

    [Test]
    public async Task GenerateFile_WithNullTemplateName_ShouldThrowArgumentException()
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        var act = async () => await _renderer.GenerateFile(null!, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task GenerateFile_WithEmptyTemplateName_ShouldThrowArgumentException()
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        var act = async () => await _renderer.GenerateFile(string.Empty, outputFile, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task GenerateFile_WithWhitespaceTemplateName_ShouldThrowArgumentException()
    {
        // Arrange
        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Act
        var act = async () => await _renderer.GenerateFile("   ", outputFile, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    [Test]
    public async Task GenerateFile_WithConfiguredTimeout_ShouldRespectConfiguration()
    {
        // Arrange - verify that configuring a generous timeout allows normal rendering
        var templateName = "normal.liquid";
        var templateContent = "Hello {{ name }}!";
        var safeTemplateName = Path.GetFileName(templateName);
        if (Path.IsPathRooted(safeTemplateName))
            throw new ArgumentException($"Template name resolves to a rooted path after sanitization: {safeTemplateName}", nameof(templateName));
        var templatePath = Path.Combine(_testTemplateDir, safeTemplateName);
        await File.WriteAllTextAsync(templatePath, templateContent);

        var outputFile = "output.txt";
        var context = new Dictionary<string, object> { { "name", "World" } };

        // Configure a 60-second timeout - should be more than enough
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TemplateRendering:TimeoutSeconds"] = "60",
                ["TemplateRendering:MaxOutputSizeBytes"] = "1048576"
            })
            .Build();

        var renderer = new UTMO.Text.FileGenerator.TemplateRenderer(
            _mockOptions.Object, _mockFileWriter.Object, _mockLogger.Object, config);

        // Act & Assert - should complete successfully
        await renderer.GenerateFile(templateName, outputFile, context);

        _mockFileWriter.Verify(fw => fw.WriteFile(outputFile, "Hello World!", false), Times.Once);
    }
}

