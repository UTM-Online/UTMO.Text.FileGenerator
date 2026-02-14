using FluentAssertions;
using Moq;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Validators;

namespace TestFileGenerator.Core.Tests.Validators;

/// <summary>
/// Tests for BasicValidators fluent validation API.
/// </summary>
[TestFixture]
public class BasicValidatorsTests
{
    private Mock<ITemplateModel> _mockModel = null!;

    [SetUp]
    public void Setup()
    {
        _mockModel = new Mock<ITemplateModel>();
        _mockModel.Setup(m => m.ResourceName).Returns("TestResource");
        _mockModel.Setup(m => m.ResourceTypeName).Returns("TestType");
    }

    [Test]
    public void ValidationBuilder_ShouldInitializeWithEmptyErrors()
    {
        // Act
        var (model, errors) = _mockModel.Object.ValidationBuilder();

        // Assert
        model.Should().BeSameAs(_mockModel.Object);
        errors.Should().BeEmpty();
    }

    [Test]
    public void ValidateNotNull_WithValidValue_ShouldNotAddError()
    {
        // Arrange
        var hook = _mockModel.Object.ValidationBuilder();
        var validValue = new object();

        // Act
        var (model, errors) = hook.ValidateNotNull(validValue, "testParam");

        // Assert
        errors.Should().BeEmpty();
        model.Should().BeSameAs(_mockModel.Object);
    }

    [Test]
    public void ValidateNotNull_WithNullValue_ShouldAddValidationError()
    {
        // Arrange
        var hook = _mockModel.Object.ValidationBuilder();

        // Act
        var (_, errors) = hook.ValidateNotNull(null, "testParam");

        // Assert
        errors.Should().HaveCount(1);
        var error = errors[0];
        error.ResourceName.Should().Be("TestResource");
        error.ResourceTypeName.Should().Be("TestType");
        error.Category.Should().Be(ValidationFailureType.InvalidResource);
        error.Message.Should().Contain("testParam").And.Contain("cannot be null");
    }

    [Test]
    public void ValidateStringNotNullOrEmpty_WithValidString_ShouldNotAddError()
    {
        // Arrange
        var hook = _mockModel.Object.ValidationBuilder();

        // Act
        var (_, errors) = hook.ValidateStringNotNullOrEmpty("valid string", "stringParam");

        // Assert
        errors.Should().BeEmpty();
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    public void ValidateStringNotNullOrEmpty_WithInvalidString_ShouldAddValidationError(string? invalidValue)
    {
        // Arrange
        var hook = _mockModel.Object.ValidationBuilder();

        // Act
        var (_, errors) = hook.ValidateStringNotNullOrEmpty(invalidValue, "stringParam");

        // Assert
        errors.Should().HaveCount(1);
        var error = errors[0];
        error.Message.Should().Contain("stringParam").And.Contain("cannot be null or empty");
    }

    [Test]
    public void ValidateArrayNotNullOrEmpty_WithValidArray_ShouldNotAddError()
    {
        // Arrange
        var hook = _mockModel.Object.ValidationBuilder();
        var validArray = new[] { 1, 2, 3 };

        // Act
        var (_, errors) = hook.ValidateArrayNotNullOrEmpty(validArray, "arrayParam");

        // Assert
        errors.Should().BeEmpty();
    }

    [Test]
    public void ValidateArrayNotNullOrEmpty_WithNullArray_ShouldAddValidationError()
    {
        // Arrange
        var hook = _mockModel.Object.ValidationBuilder();

        // Act
        var (_, errors) = hook.ValidateArrayNotNullOrEmpty<int>(null, "arrayParam");

        // Assert
        errors.Should().HaveCount(1);
        var error = errors[0];
        error.Message.Should().Contain("arrayParam").And.Contain("cannot be null or empty");
    }

    [Test]
    public void ValidateArrayNotNullOrEmpty_WithEmptyArray_ShouldAddValidationError()
    {
        // Arrange
        var hook = _mockModel.Object.ValidationBuilder();
        var emptyArray = Array.Empty<int>();

        // Act
        var (_, errors) = hook.ValidateArrayNotNullOrEmpty(emptyArray, "arrayParam");

        // Assert
        errors.Should().HaveCount(1);
    }

    [Test]
    public void FluentValidation_ChainedCalls_ShouldAccumulateErrors()
    {
        // Arrange & Act
        var (_, errors) = _mockModel.Object.ValidationBuilder()
            .ValidateNotNull(null, "param1")
            .ValidateStringNotNullOrEmpty("", "param2")
            .ValidateArrayNotNullOrEmpty<int>(null, "param3");

        // Assert
        errors.Should().HaveCount(3);
        errors.Select(e => e.Message).Should().Contain(m => m.Contains("param1"))
            .And.Contain(m => m.Contains("param2"))
            .And.Contain(m => m.Contains("param3"));
    }

    [Test]
    public void FluentValidation_MixedValidAndInvalid_ShouldOnlyAddErrorsForInvalid()
    {
        // Arrange & Act
        var (_, errors) = _mockModel.Object.ValidationBuilder()
            .ValidateNotNull(new object(), "validParam1") // valid
            .ValidateStringNotNullOrEmpty(null, "invalidParam1") // invalid
            .ValidateNotNull("valid", "validParam2") // valid
            .ValidateArrayNotNullOrEmpty(Array.Empty<int>(), "invalidParam2"); // invalid

        // Assert
        errors.Should().HaveCount(2);
        errors.Select(e => e.Message).Should().Contain(m => m.Contains("invalidParam1"))
            .And.Contain(m => m.Contains("invalidParam2"));
    }
}
