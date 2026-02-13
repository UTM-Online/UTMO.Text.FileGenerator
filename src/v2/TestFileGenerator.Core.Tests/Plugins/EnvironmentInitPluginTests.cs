using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.EnvironmentInit;

namespace TestFileGenerator.Core.Tests.Plugins;

/// <summary>
/// Tests for EnvironmentInitPlugin initialization logic.
/// </summary>
[TestFixture]
public class EnvironmentInitPluginTests
{
    private Mock<ILogger<EnvironmentInitPlugin>> _mockLogger = null!;
    private Mock<ITemplateGenerationEnvironment> _mockEnvironment = null!;
    private EnvironmentInitPlugin _plugin = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<EnvironmentInitPlugin>>();
        _mockEnvironment = new Mock<ITemplateGenerationEnvironment>();
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("TestEnvironment");
        
        _plugin = new EnvironmentInitPlugin(_mockLogger.Object);
    }

    [Test]
    public void Position_ShouldBeBefore()
    {
        // Assert
        _plugin.Position.Should().Be(UTMO.Text.FileGenerator.Abstract.PluginPosition.Before);
    }

    [Test]
    public void MaxRuntime_ShouldBeFiveMinutes()
    {
        // Assert
        _plugin.MaxRuntime.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Test]
    public async Task ProcessPlugin_WithSuccessfulInitialization_ShouldReturnTrue()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.Initialize()).Verifiable();
        _mockEnvironment.Setup(e => e.InitializeAsync()).Returns(Task.CompletedTask).Verifiable();

        // Act
        var result = await _plugin.ProcessPlugin(_mockEnvironment.Object);

        // Assert
        result.Should().BeTrue();
        _mockEnvironment.Verify(e => e.Initialize(), Times.Once);
        _mockEnvironment.Verify(e => e.InitializeAsync(), Times.Once);
    }

    [Test]
    public async Task ProcessPlugin_WhenSyncInitializationThrows_ShouldReturnFalse()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.Initialize()).Throws(new InvalidOperationException("Init failed"));

        // Act
        var result = await _plugin.ProcessPlugin(_mockEnvironment.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task ProcessPlugin_WhenAsyncInitializationThrows_ShouldReturnFalse()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.Initialize()).Verifiable();
        _mockEnvironment.Setup(e => e.InitializeAsync()).ThrowsAsync(new InvalidOperationException("Async init failed"));

        // Act
        var result = await _plugin.ProcessPlugin(_mockEnvironment.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task ProcessPlugin_ShouldLogInformationMessages()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.Initialize());
        _mockEnvironment.Setup(e => e.InitializeAsync()).Returns(Task.CompletedTask);

        // Act
        await _plugin.ProcessPlugin(_mockEnvironment.Object);

        // Assert - Verify that logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Initializing Environment")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessPlugin_OnException_ShouldLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        _mockEnvironment.Setup(e => e.Initialize()).Throws(exception);

        // Act
        await _plugin.ProcessPlugin(_mockEnvironment.Object);

        // Assert - Verify error logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error during Environment Initialization")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ProcessPlugin_ShouldCallBothSyncAndAsync()
    {
        // Arrange
        var callOrder = new List<string>();
        
        _mockEnvironment.Setup(e => e.Initialize()).Callback(() => callOrder.Add("sync"));
        _mockEnvironment.Setup(e => e.InitializeAsync()).Callback(() => callOrder.Add("async")).Returns(Task.CompletedTask);

        // Act
        await _plugin.ProcessPlugin(_mockEnvironment.Object);

        // Assert
        callOrder.Should().Equal("sync", "async");
    }
}
