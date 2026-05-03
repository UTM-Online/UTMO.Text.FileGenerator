using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using UTMO.Text.FileGenerator;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Constants;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.EnvironmentInit;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests;

/// <summary>
/// Tests that verify the <c>GenerateManifestsOnly</c> CLI flag causes
/// <see cref="FileGeneratorHost"/> to skip file generation while still
/// executing the before/after render plugins.
/// </summary>
[TestFixture]
public class FileGeneratorHostGenerateManifestsOnlyTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a fully wired <see cref="FileGeneratorHost"/> that has a single
    /// environment containing one resource, a verified <see cref="ITemplateRenderer"/>
    /// mock, and a <see cref="IGeneratorCliOptions"/> mock whose
    /// <c>GenerateManifestsOnly</c> property is controlled by the caller.
    /// </summary>
    private static (
        FileGeneratorHost host,
        Mock<ITemplateRenderer> rendererMock,
        GenerationExitCodeHolder holder)
        CreateHostWithResource(bool generateManifestsOnly)
    {
        // -- renderer mock ---------------------------------------------------
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock
            .Setup(r => r.GenerateFile(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ITemplateModel>()))
            .Returns(Task.CompletedTask);
        rendererMock
            .Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));

        // -- CLI options mock -------------------------------------------------
        var cliOptionsMock = new Mock<IGeneratorCliOptions>();
        cliOptionsMock.Setup(o => o.GenerateManifestsOnly).Returns(generateManifestsOnly);
        cliOptionsMock.Setup(o => o.OutputPath).Returns("/output");

        // -- template model (resource) mock -----------------------------------
        var resourceMock = new Mock<ITemplateModel>();
        resourceMock.Setup(r => r.ResourceName).Returns("TestResource");
        resourceMock.Setup(r => r.ResourceTypeName).Returns("TestType");
        resourceMock.Setup(r => r.TemplatePath).Returns("test.liquid");
        resourceMock.Setup(r => r.EnableGeneration).Returns(true);
        resourceMock
            .Setup(r => r.ProduceOutputPath(It.IsAny<string>()))
            .Returns("/output/TestResource.txt");
        resourceMock
            .Setup(r => r.Validate())
            .ReturnsAsync(new List<ValidationFailedException>());

        // -- environment mock -------------------------------------------------
        var envMock = new Mock<ITemplateGenerationEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("TestEnv");
        envMock
            .Setup(e => e.Resources)
            .Returns(new List<ITemplateModel> { resourceMock.Object }.AsReadOnly());
        envMock
            .Setup(e => e.EnvironmentConstants)
            .Returns(new Dictionary<string, object>());
        envMock.Setup(e => e.GeneratorOptions).Returns(cliOptionsMock.Object);
        envMock
            .Setup(e => e.Validate())
            .ReturnsAsync(new List<ValidationFailedException>());
        envMock.Setup(e => e.Initialize());
        envMock.Setup(e => e.InitializeAsync()).Returns(Task.CompletedTask);

        // -- service provider -------------------------------------------------
        var services = new ServiceCollection();

        var featureManagerMock = new Mock<IFeatureManager>();
        featureManagerMock
            .Setup(fm => fm.IsEnabledAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        services.AddSingleton(featureManagerMock.Object);
        services.AddSingleton(cliOptionsMock.Object);
        services.AddSingleton(rendererMock.Object);
        services.AddSingleton(envMock.Object);

        var provider = services.BuildServiceProvider();

        // -- host construction ------------------------------------------------
        var fileWriterMock = new Mock<IGeneralFileWriter>();
        var initPluginLogger = new Mock<ILogger<EnvironmentInitPlugin>>();
        var initPlugin = new EnvironmentInitPlugin(initPluginLogger.Object);
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var exitCodeHolder = new GenerationExitCodeHolder();

        var host = new FileGeneratorHost(
            provider,
            new Mock<ILogger<FileGeneratorHost>>().Object,
            fileWriterMock.Object,
            initPlugin,
            lifetimeMock.Object,
            exitCodeHolder);

        return (host, rendererMock, exitCodeHolder);
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Test]
    public void StartAsync_WhenGenerateManifestsOnlyIsFalse_ShouldCallGenerateFile()
    {
        // Arrange
        var (host, rendererMock, _) = CreateHostWithResource(generateManifestsOnly: false);

        // Act
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        rendererMock.Verify(
            r => r.GenerateFile(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ITemplateModel>()),
            Times.Once,
            "renderer.GenerateFile should be called when GenerateManifestsOnly is false");
    }

    [Test]
    public void StartAsync_WhenGenerateManifestsOnlyIsTrue_ShouldNotCallGenerateFile()
    {
        // Arrange
        var (host, rendererMock, _) = CreateHostWithResource(generateManifestsOnly: true);

        // Act
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        rendererMock.Verify(
            r => r.GenerateFile(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ITemplateModel>()),
            Times.Never,
            "renderer.GenerateFile should NOT be called when GenerateManifestsOnly is true");
    }

    [Test]
    public void StartAsync_WhenGenerateManifestsOnlyIsTrue_ShouldStillSucceed()
    {
        // Arrange
        var (host, _, holder) = CreateHostWithResource(generateManifestsOnly: true);

        // Act
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        holder.ExitCode.Should().Be(ExitCodes.Success,
            "skipping file generation with GenerateManifestsOnly should not cause a failure exit code");
    }

    [Test]
    public void StartAsync_WhenGenerateManifestsOnlyIsFalse_ShouldSucceed()
    {
        // Arrange
        var (host, _, holder) = CreateHostWithResource(generateManifestsOnly: false);

        // Act
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        holder.ExitCode.Should().Be(ExitCodes.Success);
    }

    [Test]
    public void StartAsync_WhenGenerateManifestsOnlyIsTrue_BeforeRenderPluginShouldStillRun()
    {
        // Arrange – register a custom before-render plugin to verify it executes
        var pluginMock = new Mock<IRenderingPipelinePlugin>();
        pluginMock.Setup(p => p.Position).Returns(PluginPosition.Before);
        pluginMock
            .Setup(p => p.HandleTemplate(It.IsAny<ITemplateModel>()))
            .ReturnsAsync(true);

        var cliOptionsMock = new Mock<IGeneratorCliOptions>();
        cliOptionsMock.Setup(o => o.GenerateManifestsOnly).Returns(true);
        cliOptionsMock.Setup(o => o.OutputPath).Returns("/output");

        var resourceMock = new Mock<ITemplateModel>();
        resourceMock.Setup(r => r.EnableGeneration).Returns(true);
        resourceMock.Setup(r => r.ResourceName).Returns("Res");
        resourceMock.Setup(r => r.ResourceTypeName).Returns("Type");
        resourceMock.Setup(r => r.TemplatePath).Returns("t.liquid");
        resourceMock.Setup(r => r.ProduceOutputPath(It.IsAny<string>())).Returns("/output/Res.txt");
        resourceMock.Setup(r => r.Validate()).ReturnsAsync(new List<ValidationFailedException>());

        var envMock = new Mock<ITemplateGenerationEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Env");
        envMock.Setup(e => e.Resources)
               .Returns(new List<ITemplateModel> { resourceMock.Object }.AsReadOnly());
        envMock.Setup(e => e.EnvironmentConstants).Returns(new Dictionary<string, object>());
        envMock.Setup(e => e.GeneratorOptions).Returns(cliOptionsMock.Object);
        envMock.Setup(e => e.Validate()).ReturnsAsync(new List<ValidationFailedException>());
        envMock.Setup(e => e.Initialize());
        envMock.Setup(e => e.InitializeAsync()).Returns(Task.CompletedTask);

        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock
            .Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));

        var featureManagerMock = new Mock<IFeatureManager>();
        featureManagerMock.Setup(fm => fm.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(false);

        var services = new ServiceCollection();
        services.AddSingleton(featureManagerMock.Object);
        services.AddSingleton(cliOptionsMock.Object);
        services.AddSingleton(rendererMock.Object);
        services.AddSingleton(envMock.Object);
        services.AddSingleton(pluginMock.Object);
        var provider = services.BuildServiceProvider();

        var initPlugin = new EnvironmentInitPlugin(new Mock<ILogger<EnvironmentInitPlugin>>().Object);
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var host = new FileGeneratorHost(
            provider,
            new Mock<ILogger<FileGeneratorHost>>().Object,
            new Mock<IGeneralFileWriter>().Object,
            initPlugin,
            lifetimeMock.Object,
            new GenerationExitCodeHolder());

        // Act
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        pluginMock.Verify(p => p.HandleTemplate(It.IsAny<ITemplateModel>()), Times.Once,
            "before-render plugins should still execute even when GenerateManifestsOnly is true");
    }

    [Test]
    public void StartAsync_WhenGenerateManifestsOnlyIsTrue_AfterRenderPluginShouldStillRun()
    {
        // Arrange – register a custom after-render plugin to verify it executes
        var pluginMock = new Mock<IRenderingPipelinePlugin>();
        pluginMock.Setup(p => p.Position).Returns(PluginPosition.After);
        pluginMock
            .Setup(p => p.HandleTemplate(It.IsAny<ITemplateModel>()))
            .ReturnsAsync(true);

        var cliOptionsMock = new Mock<IGeneratorCliOptions>();
        cliOptionsMock.Setup(o => o.GenerateManifestsOnly).Returns(true);
        cliOptionsMock.Setup(o => o.OutputPath).Returns("/output");

        var resourceMock = new Mock<ITemplateModel>();
        resourceMock.Setup(r => r.EnableGeneration).Returns(true);
        resourceMock.Setup(r => r.ResourceName).Returns("Res");
        resourceMock.Setup(r => r.ResourceTypeName).Returns("Type");
        resourceMock.Setup(r => r.TemplatePath).Returns("t.liquid");
        resourceMock.Setup(r => r.ProduceOutputPath(It.IsAny<string>())).Returns("/output/Res.txt");
        resourceMock.Setup(r => r.Validate()).ReturnsAsync(new List<ValidationFailedException>());

        var envMock = new Mock<ITemplateGenerationEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns("Env");
        envMock.Setup(e => e.Resources)
               .Returns(new List<ITemplateModel> { resourceMock.Object }.AsReadOnly());
        envMock.Setup(e => e.EnvironmentConstants).Returns(new Dictionary<string, object>());
        envMock.Setup(e => e.GeneratorOptions).Returns(cliOptionsMock.Object);
        envMock.Setup(e => e.Validate()).ReturnsAsync(new List<ValidationFailedException>());
        envMock.Setup(e => e.Initialize());
        envMock.Setup(e => e.InitializeAsync()).Returns(Task.CompletedTask);

        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock
            .Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));

        var featureManagerMock = new Mock<IFeatureManager>();
        featureManagerMock.Setup(fm => fm.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(false);

        var services = new ServiceCollection();
        services.AddSingleton(featureManagerMock.Object);
        services.AddSingleton(cliOptionsMock.Object);
        services.AddSingleton(rendererMock.Object);
        services.AddSingleton(envMock.Object);
        services.AddSingleton(pluginMock.Object);
        var provider = services.BuildServiceProvider();

        var initPlugin = new EnvironmentInitPlugin(new Mock<ILogger<EnvironmentInitPlugin>>().Object);
        var lifetimeMock = new Mock<IHostApplicationLifetime>();
        var host = new FileGeneratorHost(
            provider,
            new Mock<ILogger<FileGeneratorHost>>().Object,
            new Mock<IGeneralFileWriter>().Object,
            initPlugin,
            lifetimeMock.Object,
            new GenerationExitCodeHolder());

        // Act
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

        // Assert
        pluginMock.Verify(p => p.HandleTemplate(It.IsAny<ITemplateModel>()), Times.Once,
            "after-render plugins should still execute even when GenerateManifestsOnly is true");
    }
}


