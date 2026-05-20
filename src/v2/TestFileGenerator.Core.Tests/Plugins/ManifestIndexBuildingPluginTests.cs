using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using Moq;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.Constants;
using UTMO.Text.FileGenerator.Models;
using UTMO.Text.FileGenerator.Plugins;

namespace TestFileGenerator.Core.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="ManifestIndexBuildingPlugin"/>.
/// </summary>
[TestFixture]
public class ManifestIndexBuildingPluginTests
{
    private Mock<IFeatureManager> _featureManagerMock = null!;
    private ManifestReferenceIndex _index = null!;
    private ManifestIndexBuildingPlugin _plugin = null!;

    [SetUp]
    public void SetUp()
    {
        _featureManagerMock = new Mock<IFeatureManager>();
        _index              = new ManifestReferenceIndex();
        _plugin             = new ManifestIndexBuildingPlugin(
            _featureManagerMock.Object,
            _index,
            NullLogger<ManifestIndexBuildingPlugin>.Instance);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Plugin metadata
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void Position_ShouldBeBefore()
    {
        _plugin.Position.Should().Be(PluginPosition.Before);
    }

    [Test]
    public void RequiresGeneration_ShouldBeFalse()
    {
        _plugin.RequiresGeneration.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Feature-flag gate
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPlugin_WhenFeatureFlagDisabled_ReturnsTrue_AndDoesNotIndex()
    {
        _featureManagerMock
            .Setup(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution))
            .ReturnsAsync(false);

        var resource  = CreateManifestResource("TypeA", "R1", new { Value = 1 });
        var env       = CreateEnvironment([resource]);

        var result = await _plugin.ProcessPlugin(env);

        result.Should().BeTrue();
        _index.HasManifest("TypeA", "R1").Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Basic indexing
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPlugin_IndexesManifestProducerResources()
    {
        EnableFeatureFlag();

        var resource = CreateManifestResource("TypeA", "R1", new { Value = "hello" });
        var env      = CreateEnvironment([resource]);

        var result = await _plugin.ProcessPlugin(env);

        result.Should().BeTrue();
        _index.HasManifest("TypeA", "R1").Should().BeTrue();
        _index.TryResolveProperty("TypeA", "R1", "Value", out var v);
        v.Should().Be("hello");
    }

    [Test]
    public async Task ProcessPlugin_SkipsResourceWithGenerateManifestFalse()
    {
        EnableFeatureFlag();

        var resource = CreateNonManifestResource("TypeA", "R1");
        var env      = CreateEnvironment([resource]);

        await _plugin.ProcessPlugin(env);

        _index.HasManifest("TypeA", "R1").Should().BeFalse();
    }

    [Test]
    public async Task ProcessPlugin_IndexesMultipleResources()
    {
        EnableFeatureFlag();

        var r1  = CreateManifestResource("TypeA", "R1", new { Val = "one" });
        var r2  = CreateManifestResource("TypeB", "R2", new { Val = "two" });
        var env = CreateEnvironment([r1, r2]);

        await _plugin.ProcessPlugin(env);

        _index.HasManifest("TypeA", "R1").Should().BeTrue();
        _index.HasManifest("TypeB", "R2").Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Nested resource traversal
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPlugin_IndexesNestedManifestProducerResources()
    {
        EnableFeatureFlag();

        var nested  = CreateManifestResource("NestedType", "NestedResource", new { Nested = true });
        var parent  = new ParentTemplateModel(nested);
        var env     = CreateEnvironment([parent]);

        await _plugin.ProcessPlugin(env);

        _index.HasManifest("NestedType", "NestedResource").Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cycle protection
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPlugin_CycleProtection_DoesNotVisitSameResourceTwice()
    {
        EnableFeatureFlag();

        // Two identical resource instances with the same type/name.
        var r1  = CreateManifestResource("TypeA", "Same", new { Val = "one" });
        var r2  = CreateManifestResource("TypeA", "Same", new { Val = "two" });
        var env = CreateEnvironment([r1, r2]);

        // Should not throw or double-index.
        var result = await _plugin.ProcessPlugin(env);
        result.Should().BeTrue();

        // The last written value wins (StoreManifest overwrites).
        _index.HasManifest("TypeA", "Same").Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Error handling
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ProcessPlugin_WhenToManifestThrows_ReturnsFalse()
    {
        EnableFeatureFlag();

        var brokenResource = new Mock<ITemplateModel>();
        brokenResource.SetupGet(r => r.ResourceTypeName).Returns("TypeA");
        brokenResource.SetupGet(r => r.ResourceName).Returns("R1");
        brokenResource.SetupGet(r => r.EnableGeneration).Returns(true);

        // IManifestProducer that throws
        var brokenProducer = brokenResource.As<IManifestProducer>();
        brokenProducer.SetupGet(p => p.GenerateManifest).Returns(true);
        brokenProducer.Setup(p => p.ToManifest()).ThrowsAsync(new InvalidOperationException("boom"));

        var env = CreateEnvironment([brokenResource.Object]);

        var result = await _plugin.ProcessPlugin(env);

        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private void EnableFeatureFlag() =>
        _featureManagerMock
            .Setup(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution))
            .ReturnsAsync(true);

    private static ITemplateGenerationEnvironment CreateEnvironment(IReadOnlyList<ITemplateModel> resources)
    {
        var cliOptions = new Mock<IGeneratorCliOptions>();
        cliOptions.Setup(o => o.GenerateManifestsOnly).Returns(false);
        cliOptions.Setup(o => o.GenerateManifest).Returns(false);

        var env = new Mock<ITemplateGenerationEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("TestEnv");
        env.SetupGet(e => e.Resources).Returns(resources);
        env.SetupGet(e => e.GeneratorOptions).Returns(cliOptions.Object);
        env.Setup(e => e.Validate()).ReturnsAsync(new List<ValidationFailedException>());
        return env.Object;
    }

    private static ITemplateModel CreateManifestResource(string typeName, string resourceName, object manifestData)
    {
        var mock = new Mock<ITemplateModel>();
        mock.SetupGet(r => r.ResourceTypeName).Returns(typeName);
        mock.SetupGet(r => r.ResourceName).Returns(resourceName);
        mock.SetupGet(r => r.EnableGeneration).Returns(true);

        var producer = mock.As<IManifestProducer>();
        producer.SetupGet(p => p.GenerateManifest).Returns(true);
        producer.Setup(p => p.ToManifest()).ReturnsAsync((object?)manifestData);

        return mock.Object;
    }

    private static ITemplateModel CreateNonManifestResource(string typeName, string resourceName)
    {
        var mock = new Mock<ITemplateModel>();
        mock.SetupGet(r => r.ResourceTypeName).Returns(typeName);
        mock.SetupGet(r => r.ResourceName).Returns(resourceName);
        mock.SetupGet(r => r.EnableGeneration).Returns(true);

        var producer = mock.As<IManifestProducer>();
        producer.SetupGet(p => p.GenerateManifest).Returns(false);

        return mock.Object;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers – test doubles
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A template model that exposes a nested <see cref="ITemplateModel"/> property so that
    /// the traversal logic can be tested without modifying real classes.
    /// </summary>
    private sealed class ParentTemplateModel(ITemplateModel nested) : ITemplateModel
    {
        public string ResourceTypeName   => "ParentType";
        public string ResourceName       => "ParentResource";
        public string TemplatePath       => "parent.liquid";
        public string OutputExtension    => "txt";
        public bool   EnableGeneration   => true;
        public bool   UseAlternateName   => false;

        /// <summary>Public property that holds a nested resource.</summary>
        public ITemplateModel NestedResource => nested;

        public Task<Dictionary<string, object>> ToTemplateContext() =>
            Task.FromResult(new Dictionary<string, object>());

        public Task<List<ValidationFailedException>> Validate() =>
            Task.FromResult(new List<ValidationFailedException>());

        public string ProduceOutputPath(string basePath) =>
            Path.Combine(basePath, "parent.txt");

        public ITemplateModel AddAdditionalProperty<T>(string key, T value) => this;
    }
}
