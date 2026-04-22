using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UTMO.Text.FileGenerator;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;

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
    private Mock<IConfiguration> _mockConfiguration = null!;
    private IConfiguration _defaultConfiguration = null!;
    private UTMO.Text.FileGenerator.TemplateRenderer _renderer = null!;
    private string _testTemplateDir = null!;

    [SetUp]
    public void Setup()
    {
        _mockFileWriter = new Mock<IGeneralFileWriter>();
        _mockOptions = new Mock<IGeneratorCliOptions>();
        _mockLogger = new Mock<ILogger<UTMO.Text.FileGenerator.TemplateRenderer>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _defaultConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        
        _testTemplateDir = Path.Combine(Path.GetTempPath(), $"TemplateTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testTemplateDir);
        
        _mockOptions.Setup(o => o.TemplatePath).Returns(_testTemplateDir);
        _mockOptions.Setup(o => o.OutputPath).Returns(Path.GetTempPath());
        
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
        // Arrange - a template with a very large loop that would run for a very long time
        var templateName = "slow.liquid";
        var templateContent = "{% for i in (1..1000000000) %}{{ i }}{% endfor %}";
        var templatePath = Path.Combine(_testTemplateDir, templateName);
        await File.WriteAllTextAsync(templatePath, templateContent);

        var outputFile = "output.txt";
        var context = new Dictionary<string, object>();

        // Configure a 1-second timeout via IConfiguration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TemplateRendering:TimeoutSeconds"] = "1"
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
        // Arrange - a template that produces output exceeding the configured limit
        var templateName = "large.liquid";
        // 200 chars per iteration × 100 iterations = 20,000 chars, well above our 100-byte limit
        var templateContent = "{% for i in (1..100) %}{{ 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA' }}{% endfor %}";
        var templatePath = Path.Combine(_testTemplateDir, templateName);
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

    [Test]
    public async Task GenerateFile_WithConfiguredTimeout_ShouldRespectConfiguration()
    {
        // Arrange - verify that configuring a generous timeout allows normal rendering
        var templateName = "normal.liquid";
        var templateContent = "Hello {{ name }}!";
        var templatePath = Path.Combine(_testTemplateDir, templateName);
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

