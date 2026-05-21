using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moq;
using UTMO.Text.FileGenerator;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.EnvironmentInit;
using UTMO.Text.FileGenerator.Models;
using UTMO.Text.FileGenerator.ResourceManifestGeneration;
using UTMO.Text.FileGenerator.Abstract.Constants;

namespace TestFileGenerator.Core.Tests;

[TestFixture]
public class FileGeneratorHostGenerateManifestsOnlyTests
{
    [Test]
    public async Task StartAsync_WhenGenerateManifestsOnlyIsFalse_ShouldRenderEnabledResource()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));
        rendererMock.Setup(r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>())).Returns(Task.CompletedTask);

        var cliOptions = CreateOptions(generateManifestsOnly: false, generateManifest: false);
        var environment = CreateEnvironment(cliOptions.Object, [new TestTemplateModel(enableGeneration: true)]);

        var host = CreateHost(rendererMock.Object, environment.Object, cliOptions.Object);

        await host.StartAsync(CancellationToken.None);

        rendererMock.Verify(r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()), Times.Once);
    }

    [Test]
    public async Task StartAsync_WhenGenerateManifestsOnlyIsTrue_ShouldSkipRenderAndRenderPlugins()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));

        var beforePlugin = new Mock<IRenderingPipelinePlugin>();
        beforePlugin.SetupGet(p => p.Position).Returns(PluginPosition.Before);
        beforePlugin.SetupGet(p => p.RequiresGeneration).Returns(true);
        beforePlugin.Setup(p => p.HandleTemplate(It.IsAny<ITemplateModel>())).ReturnsAsync(true);

        var afterPlugin = new Mock<IRenderingPipelinePlugin>();
        afterPlugin.SetupGet(p => p.Position).Returns(PluginPosition.After);
        afterPlugin.SetupGet(p => p.RequiresGeneration).Returns(true);
        afterPlugin.Setup(p => p.HandleTemplate(It.IsAny<ITemplateModel>())).ReturnsAsync(true);

        var cliOptions = CreateOptions(generateManifestsOnly: true, generateManifest: false);
        var environment = CreateEnvironment(cliOptions.Object, [new TestTemplateModel(enableGeneration: true)]);

        var host = CreateHost(rendererMock.Object, environment.Object, cliOptions.Object, [beforePlugin.Object, afterPlugin.Object]);

        await host.StartAsync(CancellationToken.None);

        rendererMock.Verify(r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()), Times.Never);
        beforePlugin.Verify(p => p.HandleTemplate(It.IsAny<ITemplateModel>()), Times.Never);
        afterPlugin.Verify(p => p.HandleTemplate(It.IsAny<ITemplateModel>()), Times.Never);
    }

    [Test]
    public async Task StartAsync_WhenParallelEnabled_ShouldSkipDisabledResources()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));

        var cliOptions = CreateOptions(generateManifestsOnly: false, generateManifest: false);
        var disabledResource = new TestTemplateModel(enableGeneration: false);
        var environment = CreateEnvironment(cliOptions.Object, [disabledResource]);

        var featureManagerMock = new Mock<IFeatureManager>();
        featureManagerMock.Setup(fm => fm.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(true);

        var host = CreateHost(rendererMock.Object, environment.Object, cliOptions.Object, featureManager: featureManagerMock.Object);

        await host.StartAsync(CancellationToken.None);

        rendererMock.Verify(r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()), Times.Never);
    }

    [Test]
    public async Task StartAsync_WhenGenerateManifestsOnlyIsTrue_AndManifestReferenceResolutionFeatureIsEnabled_ShouldGenerateManifestFiles()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));

        var fileWriter = new Mock<IGeneralFileWriter>();
        fileWriter.Setup(w => w.WriteFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        var featureManagerMock = new Mock<IFeatureManager>();
        featureManagerMock.Setup(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution)).ReturnsAsync(true);

        var cliOptions = CreateOptions(generateManifestsOnly: true, generateManifest: false);
        var environment = CreateEnvironment(cliOptions.Object, [new TestTemplateModel(enableGeneration: true)]);

        var manifestPlugin = new ManifestPipelineProcessor(
            fileWriter.Object,
            Mock.Of<ILogger<ManifestPipelineProcessor>>(),
            featureManagerMock.Object);

        var host = CreateHost(rendererMock.Object, environment.Object, cliOptions.Object, pipelinePlugins: [manifestPlugin], fileWriter: fileWriter.Object);

        await host.StartAsync(CancellationToken.None);

        featureManagerMock.Verify(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution), Times.AtLeastOnce);
        fileWriter.Verify(
            w => w.WriteFile(
                It.Is<string>(path => path.Contains("Manifests") && path.EndsWith("TestType.Manifest.json")),
                It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Once);
    }

    [Test]
    public async Task StartAsync_WhenGenerateManifestsOnlyIsTrue_AndManifestReferenceResolutionFeatureIsDisabled_ShouldGenerateManifestFiles()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));

        var fileWriter = new Mock<IGeneralFileWriter>();
        fileWriter.Setup(w => w.WriteFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        var featureManagerMock = new Mock<IFeatureManager>();
        featureManagerMock.Setup(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution)).ReturnsAsync(false);

        var cliOptions = CreateOptions(generateManifestsOnly: true, generateManifest: false);
        var environment = CreateEnvironment(cliOptions.Object, [new TestTemplateModel(enableGeneration: true)]);

        var manifestPlugin = new ManifestPipelineProcessor(
            fileWriter.Object,
            Mock.Of<ILogger<ManifestPipelineProcessor>>(),
            featureManagerMock.Object);

        var host = CreateHost(rendererMock.Object, environment.Object, cliOptions.Object, pipelinePlugins: [manifestPlugin], fileWriter: fileWriter.Object);

        await host.StartAsync(CancellationToken.None);

        featureManagerMock.Verify(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution), Times.AtLeastOnce);
        fileWriter.Verify(
            w => w.WriteFile(
                It.Is<string>(path => path.Contains("Manifests") && path.EndsWith("TestType.Manifest.json")),
                It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Once);
    }

    private static Mock<IGeneratorCliOptions> CreateOptions(bool generateManifestsOnly, bool generateManifest)
    {
        var cliOptions = new Mock<IGeneratorCliOptions>();
        cliOptions.Setup(o => o.OutputPath).Returns("/output");
        cliOptions.Setup(o => o.TemplatePath).Returns("/templates");
        cliOptions.Setup(o => o.GenerateManifestsOnly).Returns(generateManifestsOnly);
        cliOptions.Setup(o => o.GenerateManifest).Returns(generateManifest);
        return cliOptions;
    }

    private static Mock<ITemplateGenerationEnvironment> CreateEnvironment(IGeneratorCliOptions cliOptions, IReadOnlyList<ITemplateModel> resources)
    {
        var environment = new Mock<ITemplateGenerationEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("TestEnvironment");
        environment.SetupGet(e => e.GeneratorOptions).Returns(cliOptions);
        environment.SetupGet(e => e.Resources).Returns(resources);
        environment.SetupGet(e => e.EnvironmentConstants).Returns(new Dictionary<string, object>());
        environment.Setup(e => e.Validate()).ReturnsAsync(new List<ValidationFailedException>());
        environment.Setup(e => e.Initialize());
        environment.Setup(e => e.InitializeAsync()).Returns(Task.CompletedTask);
        return environment;
    }

    private static FileGeneratorHost CreateHost(
        ITemplateRenderer renderer,
        ITemplateGenerationEnvironment environment,
        IGeneratorCliOptions cliOptions,
        IEnumerable<IRenderingPipelinePlugin>? renderingPlugins = null,
        IEnumerable<IPipelinePlugin>? pipelinePlugins = null,
        IFeatureManager? featureManager = null,
        IGeneralFileWriter? fileWriter = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(renderer);
        services.AddSingleton(environment);
        services.AddSingleton(cliOptions);

        if (featureManager != null)
        {
            services.AddSingleton(featureManager);
        }
        else
        {
            var defaultFeatureManager = new Mock<IFeatureManager>();
            defaultFeatureManager.Setup(fm => fm.IsEnabledAsync(It.IsAny<string>())).ReturnsAsync(false);
            services.AddSingleton(defaultFeatureManager.Object);
        }

        if (renderingPlugins != null)
        {
            foreach (var plugin in renderingPlugins)
            {
                services.AddSingleton(plugin);
            }
        }

        if (pipelinePlugins != null)
        {
            foreach (var plugin in pipelinePlugins)
            {
                services.AddSingleton(plugin);
            }
        }

        var provider = services.BuildServiceProvider();
        var initPlugin = new EnvironmentInitPlugin(Mock.Of<ILogger<EnvironmentInitPlugin>>());
        return new FileGeneratorHost(
            provider,
            Mock.Of<ILogger<FileGeneratorHost>>(),
            fileWriter ?? Mock.Of<IGeneralFileWriter>(),
            initPlugin,
            Mock.Of<IHostApplicationLifetime>(),
            new GenerationExitCodeHolder());
    }

    private sealed class TestTemplateModel(bool enableGeneration) : ITemplateModel, IManifestProducer
    {
        public string ResourceName => "TestResource";
        public string ResourceTypeName => "TestType";
        public string TemplatePath => "test.liquid";
        public bool EnableGeneration => enableGeneration;
        public string OutputExtension => ".txt";
        public bool UseAlternateName => false;
        public bool GenerateManifest => true;

        public Task<Dictionary<string, object>> ToTemplateContext() => Task.FromResult(new Dictionary<string, object>());

        public Task<List<ValidationFailedException>> Validate() => Task.FromResult(new List<ValidationFailedException>());

        public string ProduceOutputPath(string basePath) => Path.Combine(basePath, "TestResource.txt");

        public ITemplateModel AddAdditionalProperty<T>(string key, T value) => this;

        public Task<object?> ToManifest() => Task.FromResult<object?>(new { this.ResourceName, this.ResourceTypeName });
    }
}

