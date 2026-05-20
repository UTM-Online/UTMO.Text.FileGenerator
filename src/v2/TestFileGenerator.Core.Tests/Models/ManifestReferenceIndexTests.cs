using FluentAssertions;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ManifestReferenceIndex"/> property-path resolution and
/// thread-safety behaviour.
/// </summary>
[TestFixture]
public class ManifestReferenceIndexTests
{
    private ManifestReferenceIndex _index = null!;

    [SetUp]
    public void SetUp()
    {
        _index = new ManifestReferenceIndex();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // StoreManifest / HasManifest
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void HasManifest_WhenNothingStored_ReturnsFalse()
    {
        _index.HasManifest("TypeA", "ResourceA").Should().BeFalse();
    }

    [Test]
    public void HasManifest_AfterStoring_ReturnsTrue()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Foo = "bar" });
        _index.HasManifest("TypeA", "ResourceA").Should().BeTrue();
    }

    [Test]
    public void StoreManifest_Overwrites_ExistingEntry()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Value = "first" });
        _index.StoreManifest("TypeA", "ResourceA", new { Value = "second" });

        _index.TryResolveProperty("TypeA", "ResourceA", "Value", out var value);
        value.Should().Be("second");
    }

    [Test]
    public void Clear_RemovesAllEntries()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Foo = "bar" });
        _index.Clear();
        _index.HasManifest("TypeA", "ResourceA").Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TryResolveProperty – single-segment paths
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void TryResolveProperty_WhenResourceNotInIndex_ReturnsFalse()
    {
        var found = _index.TryResolveProperty("TypeA", "ResourceA", "Foo", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Test]
    public void TryResolveProperty_WithAnonymousObject_ResolvesTopLevelProperty()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { DependsOn = "BaseConfig" });

        var found = _index.TryResolveProperty("TypeA", "ResourceA", "DependsOn", out var value);

        found.Should().BeTrue();
        value.Should().Be("BaseConfig");
    }

    [Test]
    public void TryResolveProperty_WithDictionary_ResolvesTopLevelKey()
    {
        _index.StoreManifest("TypeA", "ResourceA", new Dictionary<string, object> { ["SubnetId"] = "subnet-123" });

        var found = _index.TryResolveProperty("TypeA", "ResourceA", "SubnetId", out var value);

        found.Should().BeTrue();
        value.Should().Be("subnet-123");
    }

    [Test]
    public void TryResolveProperty_WithNullableDictionary_ResolvesTopLevelKey()
    {
        _index.StoreManifest("TypeA", "ResourceA", new Dictionary<string, object?> { ["SubnetId"] = "subnet-456" });

        var found = _index.TryResolveProperty("TypeA", "ResourceA", "SubnetId", out var value);

        found.Should().BeTrue();
        value.Should().Be("subnet-456");
    }

    [Test]
    public void TryResolveProperty_WhenPropertyMissing_ReturnsFalse()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Foo = "bar" });

        var found = _index.TryResolveProperty("TypeA", "ResourceA", "NonExistent", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TryResolveProperty – nested / dot-separated paths
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void TryResolveProperty_WithNestedAnonymousObject_ResolvesNestedProperty()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Network = new { SubnetId = "sn-99" } });

        var found = _index.TryResolveProperty("TypeA", "ResourceA", "Network.SubnetId", out var value);

        found.Should().BeTrue();
        value.Should().Be("sn-99");
    }

    [Test]
    public void TryResolveProperty_WithDeeplyNestedPath_Resolves()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { A = new { B = new { C = 42 } } });

        var found = _index.TryResolveProperty("TypeA", "ResourceA", "A.B.C", out var value);

        found.Should().BeTrue();
        value.Should().Be(42);
    }

    [Test]
    public void TryResolveProperty_WhenIntermediateSegmentMissing_ReturnsFalse()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Foo = "bar" });

        var found = _index.TryResolveProperty("TypeA", "ResourceA", "Foo.Missing", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Test]
    public void TryResolveProperty_WhenIntermediateValueIsNull_ReturnsFalse()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Nested = (object?)null });

        var found = _index.TryResolveProperty("TypeA", "ResourceA", "Nested.Leaf", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TryResolveProperty – empty path
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void TryResolveProperty_WithEmptyPath_ReturnsManifestRoot()
    {
        var manifest = new { Value = 1 };
        _index.StoreManifest("TypeA", "ResourceA", manifest);

        var found = _index.TryResolveProperty("TypeA", "ResourceA", string.Empty, out var value);

        found.Should().BeTrue();
        value.Should().BeSameAs(manifest);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Lookup key is case-insensitive
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void TryResolveProperty_LookupIsCaseInsensitiveOnKey()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Value = "found" });

        var found = _index.TryResolveProperty("TYPEA", "RESOURCEA", "Value", out var value);

        found.Should().BeTrue();
        value.Should().Be("found");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Multiple resources in the index
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void TryResolveProperty_WithMultipleResources_ResolvesCorrectOne()
    {
        _index.StoreManifest("TypeA", "Resource1", new { Val = "one" });
        _index.StoreManifest("TypeA", "Resource2", new { Val = "two" });

        _index.TryResolveProperty("TypeA", "Resource1", "Val", out var v1);
        _index.TryResolveProperty("TypeA", "Resource2", "Val", out var v2);

        v1.Should().Be("one");
        v2.Should().Be("two");
    }
}
