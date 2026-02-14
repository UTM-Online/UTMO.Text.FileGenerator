using FluentAssertions;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Exceptions;

namespace TestFileGenerator.Core.Tests.Exceptions;

/// <summary>
/// Tests for custom exception classes.
/// </summary>
[TestFixture]
public class ExceptionTests
{
    [Test]
    public void ValidationFailedException_ShouldInitializeCorrectly()
    {
        // Arrange
        var resourceName = "TestResource";
        var resourceType = "TestType";
        var failureType = ValidationFailureType.InvalidResource;
        var message = "Test validation failure";

        // Act
        var exception = new ValidationFailedException(resourceName, resourceType, failureType, message);

        // Assert
        exception.ResourceName.Should().Be(resourceName);
        exception.ResourceTypeName.Should().Be(resourceType);
        exception.Category.Should().Be(failureType);
        exception.Message.Should().Contain(message);
    }

    [Test]
    public void TemplateNotFoundException_ShouldInitializeCorrectly()
    {
        // Arrange
        var templateName = "test.liquid";
        var searchPath = "/templates";

        // Act
        var exception = new TemplateNotFoundException(templateName, searchPath);

        // Assert
        exception.TemplateName.Should().Be(templateName);
        exception.TemplateSearchPath.Should().Be(searchPath);
        exception.Message.Should().Contain(templateName).And.Contain(searchPath);
    }

    [Test]
    public void TemplateRenderingException_ShouldInitializeCorrectly()
    {
        // Arrange
        var message = "Rendering failed";
        var model = new Dictionary<string, object> { { "TemplateName", "test.liquid" } };
        var outputPath = "/output/file.txt";
        var templateName = "test.liquid";

        // Act
        var exception = new TemplateRenderingException(message, model, outputPath, templateName);

        // Assert
        exception.TemplateName.Should().Be(templateName);
        exception.OutputFileName.Should().Be(outputPath);
        exception.Model.Should().BeSameAs(model);
        exception.Message.Should().Contain(message).And.Contain(outputPath);
    }

    [Test]
    public void TemplateRenderingException_WithInnerException_ShouldWrapCorrectly()
    {
        // Arrange
        var message = "Rendering failed";
        var model = new Dictionary<string, object>();
        var outputPath = "/output/file.txt";
        var templateName = "test.liquid";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new TemplateRenderingException(message, model, outputPath, templateName, innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException!.Message.Should().Be("Inner error");
    }

    [Test]
    public void NoGeneratedTextException_ShouldInitializeCorrectly()
    {
        // Arrange
        var templateName = "test.liquid";
        var outputFile = "/output/file.txt";

        // Act
        var exception = new NoGeneratedTextException(templateName, outputFile);

        // Assert
        exception.Message.Should().Contain(templateName).And.Contain(outputFile);
    }

    [Test]
    public void LocalContextKeyExistsException_ShouldInitializeCorrectly()
    {
        // Arrange
        var key = "duplicateKey";

        // Act
        var exception = new LocalContextKeyExistsException(key);

        // Assert
        exception.Key.Should().Be(key);
        exception.Message.Should().Contain(key);
    }

    [Test]
    public void ValidationFailedException_AllFailureTypes_ShouldBeSupported()
    {
        // Arrange & Act
        var invalidResource = new ValidationFailedException("res", "type", ValidationFailureType.InvalidResource, "msg1");
        var missingRequired = new ValidationFailedException("res", "type", ValidationFailureType.MissingRequiredField, "msg2");
        var invalidFormat = new ValidationFailedException("res", "type", ValidationFailureType.InvalidFormat, "msg3");

        // Assert
        invalidResource.Category.Should().Be(ValidationFailureType.InvalidResource);
        missingRequired.Category.Should().Be(ValidationFailureType.MissingRequiredField);
        invalidFormat.Category.Should().Be(ValidationFailureType.InvalidFormat);
    }
}
