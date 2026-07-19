using FluentAssertions;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="LocalManifestProvider"/>, verifying it faithfully adapts
/// <see cref="IGenerationScope"/>-based calls onto the underlying <see cref="ManifestReferenceIndex"/>
/// without changing storage/lookup semantics.
/// </summary>
[TestFixture]
public class LocalManifestProviderTests
{
    private ManifestReferenceIndex _index = null!;
    private LocalManifestProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _index = new ManifestReferenceIndex();
        _provider = new LocalManifestProvider(_index);
    }

    [Test]
    public void StoreManifest_ThenTryResolveProperty_RoundTrips()
    {
        var scope = GenerationScope.ForEnvironment("Production");

        _provider.StoreManifest(scope, "TypeA", "ResourceA", new { Value = "hello" });

        _provider.TryResolveProperty(scope, "TypeA", "ResourceA", "Value", out var value).Should().BeTrue();
        value.Should().Be("hello");
    }

    [Test]
    public void HasManifest_ReflectsUnderlyingIndexState()
    {
        var scope = GenerationScope.ForEnvironment("Production");

        _provider.HasManifest(scope, "TypeA", "ResourceA").Should().BeFalse();
        _provider.StoreManifest(scope, "TypeA", "ResourceA", new { Value = "hello" });
        _provider.HasManifest(scope, "TypeA", "ResourceA").Should().BeTrue();
    }

    [Test]
    public void StoreManifestBySubject_ThenTryResolveBySubject_RoundTrips()
    {
        var scope = GenerationScope.ForEnvironment("Production");

        _provider.StoreManifestBySubject(scope, "Subject1", null, new { Value = "world" });

        _provider.TryResolveBySubject(scope, "Subject1", null, "Value", out var value).Should().BeTrue();
        value.Should().Be("world");
        _provider.HasManifestBySubject(scope, "Subject1", null).Should().BeTrue();
    }

    [Test]
    public void DifferentEnvironmentScopes_DoNotCollide()
    {
        var prod = GenerationScope.ForEnvironment("Production");
        var dev = GenerationScope.ForEnvironment("Development");

        _provider.StoreManifest(prod, "TypeA", "ResourceA", new { Value = "prod-value" });

        _provider.HasManifest(dev, "TypeA", "ResourceA").Should().BeFalse();
        _provider.HasManifest(prod, "TypeA", "ResourceA").Should().BeTrue();
    }

    [Test]
    public void StoreManifest_WithNullScope_Throws()
    {
        var act = () => _provider.StoreManifest(null!, "TypeA", "ResourceA", null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void TryResolveProperty_WhenNothingStored_ReturnsFalse()
    {
        var scope = GenerationScope.ForEnvironment("Production");

        _provider.TryResolveProperty(scope, "TypeA", "ResourceA", "Value", out var value).Should().BeFalse();
        value.Should().BeNull();
    }
}
