using FluentAssertions;
using Moq;
using UTMO.Text.FileGenerator;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using Microsoft.Extensions.Configuration;

namespace TestFileGenerator.Core.Tests.Environment;

/// <summary>
/// Tests for GenerationEnvironmentBase validation and resource management.
/// </summary>
[TestFixture]
public class GenerationEnvironmentBaseTests
{
    private class TestEnvironment : GenerationEnvironmentBase
    {
        public TestEnvironment(IConfiguration configuration, IGeneratorCliOptions options)
            : base(configuration, options)
        {
        }

        public override string EnvironmentName => "TestEnvironment";
    }

    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<IGeneratorCliOptions> _mockOptions = null!;
    private TestEnvironment _environment = null!;

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockOptions = new Mock<IGeneratorCliOptions>();
        _mockOptions.Setup(o => o.OutputPath).Returns("/output");
        _mockOptions.Setup(o => o.TemplatePath).Returns("/templates");
        
        _environment = new TestEnvironment(_mockConfiguration.Object, _mockOptions.Object);
    }

    [Test]
    public void EnvironmentName_ShouldReturnExpectedName()
    {
        // Assert
        _environment.EnvironmentName.Should().Be("TestEnvironment");
    }

    [Test]
    public void Resources_Initially_ShouldBeEmpty()
    {
        // Assert
        _environment.Resources.Should().BeEmpty();
    }

    [Test]
    public void AddResource_WithValidModel_ShouldAddToCollection()
    {
        // Arrange
        var mockModel = new Mock<ITemplateModel>();

        // Act
        var result = _environment.AddResource(mockModel.Object);

        // Assert
        result.Should().BeSameAs(_environment);
        _environment.Resources.Should().HaveCount(1);
        _environment.Resources[0].Should().BeSameAs(mockModel.Object);
    }

    [Test]
    public void AddResource_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _environment.AddResource<ITemplateModel>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void AddResource_Generic_ShouldCreateAndAddInstance()
    {
        // Arrange & Act
        var result = _environment.AddResource<TestTemplateModel>();

        // Assert
        result.Should().BeSameAs(_environment);
        _environment.Resources.Should().HaveCount(1);
        _environment.Resources[0].Should().BeOfType<TestTemplateModel>();
    }

    [Test]
    public void AddResources_WithMultipleModels_ShouldAddAll()
    {
        // Arrange
        var models = new[]
        {
            Mock.Of<ITemplateModel>(),
            Mock.Of<ITemplateModel>(),
            Mock.Of<ITemplateModel>()
        };

        // Act
        var result = _environment.AddResources(models);

        // Assert
        result.Should().BeSameAs(_environment);
        _environment.Resources.Should().HaveCount(3);
    }

    [Test]
    public void AddEnvironmentContext_ShouldAddToDictionary()
    {
        // Act
        var result = _environment.AddEnvironmentContext("key1", "value1");

        // Assert
        result.Should().BeSameAs(_environment);
        _environment.EnvironmentConstants.Should().ContainKey("key1");
        _environment.EnvironmentConstants["key1"].Should().Be("value1");
    }

    [Test]
    public async Task Validate_WithNoResources_ShouldReturnEmpty()
    {
        // Act
        var errors = await _environment.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Test]
    public async Task Validate_WithValidResources_ShouldReturnEmpty()
    {
        // Arrange
        var mockModel = new Mock<ITemplateModel>();
        mockModel.Setup(m => m.Validate()).ReturnsAsync(new List<ValidationFailedException>());
        _environment.AddResource(mockModel.Object);

        // Act
        var errors = await _environment.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Test]
    public async Task Validate_WithInvalidResource_ShouldReturnErrors()
    {
        // Arrange
        var mockModel = new Mock<ITemplateModel>();
        mockModel.Setup(m => m.ResourceName).Returns("TestResource");
        mockModel.Setup(m => m.ResourceTypeName).Returns("TestType");
        
        var validationError = new ValidationFailedException(
            "TestResource", 
            "TestType", 
            ValidationFailureType.MissingRequiredField, 
            "Field is required");
        
        mockModel.Setup(m => m.Validate()).ReturnsAsync(new List<ValidationFailedException> { validationError });
        _environment.AddResource(mockModel.Object);

        // Act
        var errors = await _environment.Validate();

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Should().BeSameAs(validationError);
    }

    [Test]
    public async Task Validate_WithMultipleResourcesAndErrors_ShouldAccumulateErrors()
    {
        // Arrange
        var mockModel1 = new Mock<ITemplateModel>();
        var error1 = new ValidationFailedException("R1", "T1", ValidationFailureType.InvalidResource, "Error 1");
        mockModel1.Setup(m => m.Validate()).ReturnsAsync(new List<ValidationFailedException> { error1 });
        
        var mockModel2 = new Mock<ITemplateModel>();
        var error2 = new ValidationFailedException("R2", "T2", ValidationFailureType.InvalidFormat, "Error 2");
        mockModel2.Setup(m => m.Validate()).ReturnsAsync(new List<ValidationFailedException> { error2 });

        _environment.AddResource(mockModel1.Object);
        _environment.AddResource(mockModel2.Object);

        // Act
        var errors = await _environment.Validate();

        // Assert
        errors.Should().HaveCount(2);
        errors.Should().Contain(error1);
        errors.Should().Contain(error2);
    }

    // Test model implementation
    private class TestTemplateModel : ITemplateModel
    {
        public string ResourceName => "Test";
        public string ResourceTypeName => "TestType";
        public string TemplatePath => "test.liquid";
        public bool EnableGeneration => true;

        public Task<Dictionary<string, object>> ToTemplateContext()
        {
            return Task.FromResult(new Dictionary<string, object>());
        }

        public Task<List<ValidationFailedException>> Validate()
        {
            return Task.FromResult(new List<ValidationFailedException>());
        }

        public string ProduceOutputPath(string basePath)
        {
            return Path.Combine(basePath, "output.txt");
        }
    }
}
