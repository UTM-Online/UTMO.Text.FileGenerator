using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using Moq;
using UTMO.Text.FileGenerator;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Constants;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.Constants;
using UTMO.Text.FileGenerator.EnvironmentInit;
using UTMO.Text.FileGenerator.Models;
using UTMO.Text.FileGenerator.Plugins;

namespace TestFileGenerator.Core.Tests.Integration;

/// <summary>
/// End-to-end integration tests verifying that the manifest reference resolution pipeline
/// (index building + resolver) works correctly when wired into <see cref="FileGeneratorHost"/>.
/// </summary>
[TestFixture]
public class ManifestReferenceResolutionIntegrationTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Feature-flag OFF – regression / backwards-compatibility
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task WhenFeatureFlagDisabled_ExistingGenerationBehaviourIsUnchanged()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));
        rendererMock.Setup(r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()))
                    .Returns(Task.CompletedTask);

        var cliOptions  = CreateOptions(generateManifestsOnly: false, generateManifest: false);
        var resource    = new CapturingResource("R1", "TypeA");
        var environment = CreateEnvironment(cliOptions, [resource]);

        var index = new ManifestReferenceIndex();
        var host  = CreateHost(rendererMock.Object, environment, cliOptions, index, featureFlagEnabled: false);

        await host.StartAsync(CancellationToken.None);

        rendererMock.Verify(
            r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()),
            Times.Once,
            "existing generation should still run when feature flag is off");

        index.HasManifest("TypeA", "R1").Should().BeFalse("index should be empty when flag is off");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Feature-flag ON – happy path
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task WhenFeatureFlagEnabled_ManifestReferenceIsResolvedBeforeRender()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));
        rendererMock.Setup(r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()))
                    .Returns(Task.CompletedTask);

        var cliOptions = CreateOptions(generateManifestsOnly: false, generateManifest: false);

        // The source resource declares a manifest reference to the "DependsOn" property of TypeB/R2.
        var sourceResource = new CapturingResource("R1", "TypeA");
        sourceResource.DeclareManifestReference("DependsOn", new ManifestReference
        {
            ResourceTypeName = "TypeB",
            ResourceName     = "R2",
            PropertyPath     = "DependsOn"
        });

        // The referenced resource returns a manifest with the "DependsOn" property.
        var referencedResource = new ManifestProducingResource("R2", "TypeB",
            () => Task.FromResult<IManifest?>(new DependsOnManifest { DependsOn = "[TypeA]R1" }));

        var environment = CreateEnvironment(cliOptions, [sourceResource, referencedResource]);

        var index = new ManifestReferenceIndex();
        var host  = CreateHost(rendererMock.Object, environment, cliOptions, index, featureFlagEnabled: true);

        await host.StartAsync(CancellationToken.None);

        host.ExitCode.Should().Be(ExitCodes.Success, "generation should succeed");

        // Scope to "TestEnv" before querying the index (AsyncLocal is per async context).
        index.BeginEnvironmentScope("TestEnv");

        // After StartAsync the index should contain the referenced resource's manifest.
        index.HasManifest("TypeB", "R2").Should().BeTrue();

        // The source resource should have had the resolved value injected.
        sourceResource.InjectedProperties.Should().ContainKey("DependsOn");
        sourceResource.InjectedProperties["DependsOn"].Should().Be("[TypeA]R1");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Feature-flag ON – required reference unresolved → generation fails
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task WhenFeatureFlagEnabled_UnresolvedRequiredReference_CausesGenerationFailure()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));
        rendererMock.Setup(r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()))
                    .Returns(Task.CompletedTask);

        var cliOptions = CreateOptions(generateManifestsOnly: false, generateManifest: false);

        var sourceResource = new CapturingResource("R1", "TypeA");
        sourceResource.DeclareManifestReference("MissingRef", new ManifestReference
        {
            ResourceTypeName = "TypeZ",     // not in the environment
            ResourceName     = "NonExistent",
            PropertyPath     = "Value",
            DefaultValue     = null         // required
        });

        var environment = CreateEnvironment(cliOptions, [sourceResource]);

        var index = new ManifestReferenceIndex();
        var host  = CreateHost(rendererMock.Object, environment, cliOptions, index, featureFlagEnabled: true);

        await host.StartAsync(CancellationToken.None);

        host.ExitCode.Should().Be(ExitCodes.GenerationErrors, "unresolved required reference should set exit code to GenerationErrors");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Feature-flag ON – optional reference uses default value
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task WhenFeatureFlagEnabled_UnresolvedOptionalReference_UsesDefaultValue()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));
        rendererMock.Setup(r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()))
                    .Returns(Task.CompletedTask);

        var cliOptions = CreateOptions(generateManifestsOnly: false, generateManifest: false);

        var sourceResource = new CapturingResource("R1", "TypeA");
        sourceResource.DeclareManifestReference("OptionalKey", new ManifestReference
        {
            ResourceTypeName = "TypeZ",
            ResourceName     = "NonExistent",
            PropertyPath     = "Value",
            DefaultValue     = "defaultFallback"
        });

        var environment = CreateEnvironment(cliOptions, [sourceResource]);

        var index = new ManifestReferenceIndex();
        var host  = CreateHost(rendererMock.Object, environment, cliOptions, index, featureFlagEnabled: true);

        await host.StartAsync(CancellationToken.None);

        host.ExitCode.Should().Be(ExitCodes.Success, "optional reference with default should not fail generation");
        sourceResource.InjectedProperties.Should().ContainKey("OptionalKey");
        sourceResource.InjectedProperties["OptionalKey"].Should().Be("defaultFallback");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GenerateManifestsOnly – index and resolver run without rendering
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task WhenGenerateManifestsOnly_IndexIsBuiltButRenderIsSkipped()
    {
        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(r => r.AddToGlobalContext(It.IsAny<Dictionary<string, object>>()));

        var cliOptions = CreateOptions(generateManifestsOnly: true, generateManifest: true);

        var referencedResource = new ManifestProducingResource("R2", "TypeB",
            () => Task.FromResult<IManifest?>(new ValManifest { Value = "indexedValue" }));

        var environment = CreateEnvironment(cliOptions, [referencedResource]);
        var index = new ManifestReferenceIndex();
        var host  = CreateHost(rendererMock.Object, environment, cliOptions, index, featureFlagEnabled: true);

        await host.StartAsync(CancellationToken.None);

        rendererMock.Verify(
            r => r.GenerateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ITemplateModel>()),
            Times.Never,
            "rendering should be skipped for GenerateManifestsOnly");

        // Index should have been built by ManifestIndexBuildingPlugin (RequiresGeneration=false).
        index.BeginEnvironmentScope("TestEnv");
        index.HasManifest("TypeB", "R2").Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static IGeneratorCliOptions CreateOptions(bool generateManifestsOnly, bool generateManifest)
    {
        var mock = new Mock<IGeneratorCliOptions>();
        mock.Setup(o => o.OutputPath).Returns("/output");
        mock.Setup(o => o.TemplatePath).Returns("/templates");
        mock.Setup(o => o.GenerateManifestsOnly).Returns(generateManifestsOnly);
        mock.Setup(o => o.GenerateManifest).Returns(generateManifest);
        return mock.Object;
    }

    private static ITemplateGenerationEnvironment CreateEnvironment(
        IGeneratorCliOptions cliOptions,
        IReadOnlyList<ITemplateModel> resources)
    {
        var env = new Mock<ITemplateGenerationEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("TestEnv");
        env.SetupGet(e => e.GeneratorOptions).Returns(cliOptions);
        env.SetupGet(e => e.Resources).Returns(resources);
        env.SetupGet(e => e.EnvironmentConstants).Returns(new Dictionary<string, object>());
        env.Setup(e => e.Validate()).ReturnsAsync(new List<ValidationFailedException>());
        env.Setup(e => e.Initialize());
        env.Setup(e => e.InitializeAsync()).Returns(Task.CompletedTask);
        return env.Object;
    }

    private static FileGeneratorHost CreateHost(
        ITemplateRenderer renderer,
        ITemplateGenerationEnvironment environment,
        IGeneratorCliOptions cliOptions,
        ManifestReferenceIndex index,
        bool featureFlagEnabled)
    {
        var featureManager = new Mock<IFeatureManager>();
        featureManager
            .Setup(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution))
            .ReturnsAsync(featureFlagEnabled);
        // All other feature flags disabled.
        featureManager
            .Setup(fm => fm.IsEnabledAsync(It.Is<string>(s => s != FeatureFlags.EnableManifestReferenceResolution)))
            .ReturnsAsync(false);

        var services = new ServiceCollection();
        services.AddSingleton(renderer);
        services.AddSingleton(environment);
        services.AddSingleton(cliOptions);
        services.AddSingleton<IFeatureManager>(featureManager.Object);
        services.AddSingleton<IManifestReferenceIndex>(index);

        // Register the index building pipeline plugin.
        services.AddSingleton<IPipelinePlugin>(
            new ManifestIndexBuildingPlugin(
                featureManager.Object,
                index,
                NullLogger<ManifestIndexBuildingPlugin>.Instance));

        // Register the resolver rendering plugin.
        services.AddSingleton<IRenderingPipelinePlugin>(
            new ManifestReferenceResolverPlugin(
                featureManager.Object,
                index,
                NullLogger<ManifestReferenceResolverPlugin>.Instance));

        var provider   = services.BuildServiceProvider();
        var initPlugin = new EnvironmentInitPlugin(Mock.Of<ILogger<EnvironmentInitPlugin>>());

        return new FileGeneratorHost(
            provider,
            Mock.Of<ILogger<FileGeneratorHost>>(),
            Mock.Of<IGeneralFileWriter>(),
            initPlugin,
            Mock.Of<IHostApplicationLifetime>(),
            new GenerationExitCodeHolder());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test doubles
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A <see cref="TemplateResourceBase"/> that exposes
    /// <c>AddManifestReference</c> publicly (for test setup) and captures
    /// injected additional properties.
    /// </summary>
    private sealed class CapturingResource(string name, string typeName) : TemplateResourceBase
    {
        public override string ResourceTypeName => typeName;
        public override string TemplatePath     => "test.liquid";
        public override string OutputExtension  => "txt";
        public override string ResourceName     => name;

        public Dictionary<string, object?> InjectedProperties { get; } = new();

        public void DeclareManifestReference(string key, ManifestReference reference) =>
            AddManifestReference(key, reference);

        public override ITemplateModel AddAdditionalProperty<T>(string key, T value)
        {
            InjectedProperties[key] = value;
            return base.AddAdditionalProperty(key, value);
        }
    }

    /// <summary>
    /// A template resource that implements <see cref="IManifestProducer"/> and returns
    /// caller-supplied manifest data from <c>ToManifest&lt;TManifest&gt;()</c>.
    /// </summary>
    private sealed class ManifestProducingResource(
        string name,
        string typeName,
        Func<Task<IManifest?>> manifestFactory) : TemplateResourceBase, IManifestProducer
    {
        public override string ResourceTypeName  => typeName;
        public override string TemplatePath      => "test.liquid";
        public override string OutputExtension   => "txt";
        public override string ResourceName      => name;
        public override bool   GenerateManifest  => true;

        public override async Task<TManifest?> ToManifest<TManifest>() where TManifest : class
        {
            var manifest = await manifestFactory();
            if (manifest is null)
            {
                return null;
            }

            if (manifest is TManifest typedManifest)
            {
                return typedManifest;
            }

            throw new InvalidCastException($"Manifest of type '{manifest.GetType().Name}' is not assignable to '{typeof(TManifest).Name}'.");
        }
    }

    private sealed class DependsOnManifest : ManifestBase
    {
        public required string DependsOn { get; init; }
    }

    private sealed class ValManifest : ManifestBase
    {
        public required string Value { get; init; }
    }
}
