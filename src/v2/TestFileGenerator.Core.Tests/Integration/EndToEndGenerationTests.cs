using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UTMO.Text.FileGenerator;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;

namespace TestFileGenerator.Core.Tests.Integration;

/// <summary>
/// Integration tests for end-to-end template generation scenarios.
/// </summary>
[TestFixture]
public class EndToEndGenerationTests
{
    private string _testTemplateDir = null!;
    private string _testOutputDir = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<IGeneratorCliOptions> _mockOptions = null!;

    [SetUp]
    public void Setup()
    {
        _testTemplateDir = Path.Combine(Path.GetTempPath(), $"TemplateDir_{Guid.NewGuid():N}");
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"OutputDir_{Guid.NewGuid():N}");
        
        Directory.CreateDirectory(_testTemplateDir);
        Directory.CreateDirectory(_testOutputDir);
        
        _mockConfiguration = new Mock<IConfiguration>();
        _mockOptions = new Mock<IGeneratorCliOptions>();
        _mockOptions.Setup(o => o.TemplatePath).Returns(_testTemplateDir);
        _mockOptions.Setup(o => o.OutputPath).Returns(_testOutputDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testTemplateDir))
        {
            Directory.Delete(_testTemplateDir, recursive: true);
        }
        
        if (Directory.Exists(_testOutputDir))
        {
            Directory.Delete(_testOutputDir, recursive: true);
        }
    }

    [Test]
    public async Task FullGenerationWorkflow_WithSimpleTemplate_ShouldCreateOutputFile()
    {
        // Arrange
        var templateContent = "Hello {{ name }}!";
        var templatePath = Path.Combine(_testTemplateDir, "greeting.liquid");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var mockFileWriter = new UTMO.Text.FileGenerator.DefaultFileWriter.DefaultFileWriter();
        var mockLogger = Mock.Of<ILogger<UTMO.Text.FileGenerator.TemplateRenderer>>();
        
        var renderer = new UTMO.Text.FileGenerator.TemplateRenderer(_mockOptions.Object, mockFileWriter, mockLogger);
        
        var outputPath = Path.Combine(_testOutputDir, "output.txt");
        var context = new Dictionary<string, object> { { "name", "World" } };

        // Act
        await renderer.GenerateFile("greeting.liquid", outputPath, context);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Be("Hello World!");
    }

    [Test]
    public async Task GenerationEnvironment_WithMultipleResources_ShouldValidateAll()
    {
        // Arrange
        var environment = new TestGenerationEnvironment(_mockConfiguration.Object, _mockOptions.Object);
        
        var validModel = new TestTemplateModel("valid", true);
        var invalidModel = new TestTemplateModel("invalid", false);
        
        environment.AddResource(validModel);
        environment.AddResource(invalidModel);

        // Act
        var errors = await environment.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].ResourceName.Should().Be("invalid");
    }

    [Test]
    public void GenerationEnvironment_WithEnvironmentConstants_ShouldMakeAvailableToResources()
    {
        // Arrange
        var environment = new TestGenerationEnvironment(_mockConfiguration.Object, _mockOptions.Object);
        
        environment.AddEnvironmentContext("GlobalSetting", "GlobalValue");
        environment.AddEnvironmentContext("Version", "1.0.0");

        // Assert
        environment.EnvironmentConstants.Should().ContainKey("GlobalSetting");
        environment.EnvironmentConstants.Should().ContainKey("Version");
        environment.EnvironmentConstants["GlobalSetting"].Should().Be("GlobalValue");
        environment.EnvironmentConstants["Version"].Should().Be("1.0.0");
    }

    [Test]
    public async Task ComplexTemplate_WithLoopsAndConditions_ShouldRenderCorrectly()
    {
        // Arrange
        var templateContent = @"
{% if items.size > 0 %}
Items:
{% for item in items %}
  - {{ item.name }}: {{ item.price }}
{% endfor %}
Total: {{ total }}
{% else %}
No items found.
{% endif %}";
        
        var templatePath = Path.Combine(_testTemplateDir, "invoice.liquid");
        await File.WriteAllTextAsync(templatePath, templateContent);

        var mockFileWriter = new UTMO.Text.FileGenerator.DefaultFileWriter.DefaultFileWriter();
        var mockLogger = Mock.Of<ILogger<UTMO.Text.FileGenerator.TemplateRenderer>>();
        var renderer = new UTMO.Text.FileGenerator.TemplateRenderer(_mockOptions.Object, mockFileWriter, mockLogger);
        
        var outputPath = Path.Combine(_testOutputDir, "invoice.txt");
        var context = new Dictionary<string, object>
        {
            {
                "items", new List<Dictionary<string, object>>
                {
                    new() { { "name", "Widget" }, { "price", "$10" } },
                    new() { { "name", "Gadget" }, { "price", "$20" } }
                }
            },
            { "total", "$30" }
        };

        // Act
        await renderer.GenerateFile("invoice.liquid", outputPath, context);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("Widget: $10");
        content.Should().Contain("Gadget: $20");
        content.Should().Contain("Total: $30");
    }

    // Test implementations
    private class TestGenerationEnvironment : GenerationEnvironmentBase
    {
        public TestGenerationEnvironment(IConfiguration configuration, IGeneratorCliOptions options)
            : base(configuration, options)
        {
        }

        public override string EnvironmentName => "TestEnvironment";
    }

    private class TestTemplateModel : ITemplateModel
    {
        private readonly bool _isValid;

        public TestTemplateModel(string name, bool isValid)
        {
            ResourceName = name;
            _isValid = isValid;
        }

        public string ResourceName { get; }
        public string ResourceTypeName => "TestType";
        public string TemplatePath => "test.liquid";
        public bool EnableGeneration => true;
        public string OutputExtension => ".json";
        public bool UseAlternateName => false;

        public Task<Dictionary<string, object>> ToTemplateContext()
        {
            return Task.FromResult(new Dictionary<string, object>
            {
                { "name", ResourceName }
            });
        }

        public Task<List<ValidationFailedException>> Validate()
        {
            var errors = new List<ValidationFailedException>();
            
            if (!_isValid)
            {
                errors.Add(new ValidationFailedException(
                    ResourceName,
                    ResourceTypeName,
                    ValidationFailureType.InvalidResource,
                    "Model is invalid"));
            }
            
            return Task.FromResult(errors);
        }

        public string ProduceOutputPath(string basePath)
        {
            return Path.Combine(basePath, $"{ResourceName}.txt");
        }

        public ITemplateModel AddAdditionalProperty<T>(string key, T value)
        {
            return this;
        }
    }
}
