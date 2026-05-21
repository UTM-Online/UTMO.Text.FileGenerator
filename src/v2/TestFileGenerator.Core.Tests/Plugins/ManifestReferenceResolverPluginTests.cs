using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using Moq;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Constants;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Constants;
using UTMO.Text.FileGenerator.Models;
using UTMO.Text.FileGenerator.Plugins;

namespace TestFileGenerator.Core.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="ManifestReferenceResolverPlugin"/>.
/// </summary>
[TestFixture]
public class ManifestReferenceResolverPluginTests
{
    private Mock<IFeatureManager> _featureManagerMock = null!;
    private ManifestReferenceIndex _index = null!;
    private ManifestReferenceResolverPlugin _plugin = null!;

    [SetUp]
    public void SetUp()
    {
        _featureManagerMock = new Mock<IFeatureManager>();
        _index              = new ManifestReferenceIndex();
        _plugin             = new ManifestReferenceResolverPlugin(
            _featureManagerMock.Object,
            _index,
            NullLogger<ManifestReferenceResolverPlugin>.Instance);
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
    public async Task HandleTemplate_WhenFeatureFlagDisabled_ReturnsTrue_AndDoesNotResolve()
    {
        _featureManagerMock
            .Setup(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution))
            .ReturnsAsync(false);

        // Index has data but plugin should be skipped.
        _index.StoreManifest("TypeA", "R1", new { Value = "indexed" });

        var resource = new TestResource("R1", "TypeA");
        resource.AddManifestReference("ContextKey", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "Value"
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeTrue();
        // The additional property should NOT have been injected.
        resource.TemplateContextKeys.Should().NotContain("ContextKey");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Non-TemplateResourceBase models are skipped
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task HandleTemplate_WhenModelIsNotTemplateResourceBase_ReturnsTrue()
    {
        EnableFeatureFlag();

        var model = new Mock<ITemplateModel>();
        var result = await _plugin.HandleTemplate(model.Object);

        result.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Successful resolution
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task HandleTemplate_ResolvesRequiredReference_InjectsValue()
    {
        EnableFeatureFlag();

        _index.StoreManifest("TypeA", "R1", new { DependsOn = "BaseConfig" });

        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("DependsOn", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "DependsOn"
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeTrue();
        resource.TemplateContextKeys.Should().Contain("DependsOn");
        resource.GetAdditionalProperty("DependsOn").Should().Be("BaseConfig");
    }

    [Test]
    public async Task HandleTemplate_ResolvesNestedPath_InjectsValue()
    {
        EnableFeatureFlag();

        _index.StoreManifest("TypeA", "R1", new { Network = new { SubnetId = "sn-abc" } });

        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("SubnetId", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "Network.SubnetId"
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeTrue();
        resource.GetAdditionalProperty("SubnetId").Should().Be("sn-abc");
    }

    [Test]
    public async Task HandleTemplate_ResolvesMultipleReferences()
    {
        EnableFeatureFlag();

        _index.StoreManifest("TypeA", "R1", new { Val1 = "one" });
        _index.StoreManifest("TypeB", "R2", new { Val2 = "two" });

        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("Key1", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "Val1"
        });
        resource.AddManifestReference("Key2", new ManifestReference
        {
            ResourceTypeName = "TypeB",
            ResourceName     = "R2",
            PropertyPath     = "Val2"
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeTrue();
        resource.GetAdditionalProperty("Key1").Should().Be("one");
        resource.GetAdditionalProperty("Key2").Should().Be("two");
    }

    [Test]
    public async Task HandleTemplate_WithGenericReference_UsesMapperToInjectValue()
    {
        EnableFeatureFlag();

        // The index is keyed by the referenced resource's ResourceTypeName, mirroring
        // how ManifestIndexBuildingPlugin stores entries: _index.StoreManifest(resource.ResourceTypeName, resource.ResourceName, manifestData).
        _index.StoreManifest("NodeConfiguration", "R1", new ManifestModel
        {
            DependsOn = "BaseConfig"
        });

        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("DependsOn", new ManifestReference<ManifestModel>(
            "NodeConfiguration",
            "R1",
            source => source.DependsOn));

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeTrue();
        resource.GetAdditionalProperty("DependsOn").Should().Be("BaseConfig");
    }

    [Test]
    public async Task HandleTemplate_WithGenericReference_WhenManifestTypeDoesNotMatch_ReturnsFalse()
    {
        EnableFeatureFlag();

        _index.StoreManifest("NodeConfiguration", "R1", new WrongManifestModel
        {
            DependsOn = "BaseConfig"
        });

        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("DependsOn", new ManifestReference<ManifestModel>(
            "NodeConfiguration",
            "R1",
            source => source.DependsOn));

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Optional reference with default value
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task HandleTemplate_WhenOptionalReferenceUnresolved_UsesDefaultValue()
    {
        EnableFeatureFlag();

        // Nothing in the index for TypeA/R1.
        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("OptionalKey", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "Value",
            DefaultValue     = "fallback"
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeTrue();
        resource.GetAdditionalProperty("OptionalKey").Should().Be("fallback");
    }

    [Test]
    public async Task HandleTemplate_WhenOptionalReferenceUnresolved_EmptyStringDefault_UsesEmptyString()
    {
        EnableFeatureFlag();

        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("OptionalKey", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "Value",
            DefaultValue     = string.Empty
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeTrue();
        resource.GetAdditionalProperty("OptionalKey").Should().Be(string.Empty);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Required reference not in index → failure
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task HandleTemplate_WhenRequiredReferenceUnresolved_ReturnsFalse()
    {
        EnableFeatureFlag();

        // Nothing in the index.
        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("RequiredKey", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "Value"
            // DefaultValue is null → required
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeFalse();
    }

    [Test]
    public async Task HandleTemplate_WhenRequiredReferencePropertyMissing_ReturnsFalse()
    {
        EnableFeatureFlag();

        // Index has the resource but not the path.
        _index.StoreManifest("TypeA", "R1", new { OtherProp = "something" });

        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("MissingProp", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "MissingProp"
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeFalse();
    }

    [Test]
    public async Task HandleTemplate_WhenSomeRequiredReferencesUnresolved_ReturnsFalse_AndResolvesOthers()
    {
        EnableFeatureFlag();

        _index.StoreManifest("TypeA", "R1", new { Good = "value" });

        var resource = new TestResource("MyResource", "MyType");
        resource.AddManifestReference("GoodKey", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "R1",
            PropertyPath     = "Good"
        });
        resource.AddManifestReference("BadKey", new ManifestReference
        {
            ResourceTypeName = "TypeA",
            ResourceName     = "MISSING",
            PropertyPath     = "Value"
        });

        var result = await _plugin.HandleTemplate(resource);

        result.Should().BeFalse();
        // The successfully resolved reference should still have been injected.
        resource.GetAdditionalProperty("GoodKey").Should().Be("value");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // No references declared → fast-path success
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task HandleTemplate_WithNoReferences_ReturnsTrue()
    {
        EnableFeatureFlag();

        var resource = new TestResource("MyResource", "MyType");
        var result   = await _plugin.HandleTemplate(resource);

        result.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private void EnableFeatureFlag() =>
        _featureManagerMock
            .Setup(fm => fm.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution))
            .ReturnsAsync(true);

    // ──────────────────────────────────────────────────────────────────────────
    // Test double
    // ──────────────────────────────────────────────────────────────────────────

    private sealed class TestResource(string resourceName, string resourceTypeName) : TemplateResourceBase
    {
        private readonly Dictionary<string, object?> _additionalProps = new();

        public override string ResourceTypeName => resourceTypeName;
        public override string TemplatePath     => "test.liquid";
        public override string OutputExtension  => "txt";
        public override string ResourceName     => resourceName;

        /// <summary>
        /// Exposes <see cref="TemplateResourceBase.AddManifestReference"/> for testing.
        /// </summary>
        public new void AddManifestReference(string key, ManifestReference reference) =>
            base.AddManifestReference(key, reference);

        /// <summary>Captures injected properties for assertion.</summary>
        public override ITemplateModel AddAdditionalProperty<T>(string key, T value)
        {
            _additionalProps[key] = value;
            return base.AddAdditionalProperty(key, value);
        }

        public IEnumerable<string> TemplateContextKeys => _additionalProps.Keys;

        public object? GetAdditionalProperty(string key) =>
            _additionalProps.TryGetValue(key, out var v) ? v : null;
    }

    private sealed class ManifestModel : ManifestBase
    {
        public required string DependsOn { get; init; }
    }

    private sealed class WrongManifestModel : ManifestBase
    {
        public required string DependsOn { get; init; }
    }
}
