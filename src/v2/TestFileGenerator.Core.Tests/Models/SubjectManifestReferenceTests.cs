using FluentAssertions;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for subject-based manifest references
/// (<see cref="ManifestReference(string, string?)"/>) and the subject-keyed index API.
/// </summary>
[TestFixture]
public class SubjectManifestReferenceTests
{
    private ManifestReferenceIndex _index = null!;

    [SetUp]
    public void SetUp() => _index = new ManifestReferenceIndex();

    // ──────────────────────────────────────────────────────────────────────────
    // Index: subject storage / lookup
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void HasManifestBySubject_WhenNothingStored_ReturnsFalse() =>
        _index.HasManifestBySubject("BaseConfig", null).Should().BeFalse();

    [Test]
    public void StoreAndResolveBySubject_RootScope_Works()
    {
        _index.StoreManifestBySubject("BaseConfig", null, new { DependsOn = "[Type]R1" });

        _index.HasManifestBySubject("BaseConfig", null).Should().BeTrue();
        _index.TryResolveBySubject("BaseConfig", null, "DependsOn", out var value).Should().BeTrue();
        value.Should().Be("[Type]R1");
    }

    [Test]
    public void ResolveBySubject_EmptyPath_ReturnsWholeManifest()
    {
        var manifest = new { Value = "v" };
        _index.StoreManifestBySubject("S", null, manifest);

        _index.TryResolveBySubject("S", null, string.Empty, out var value).Should().BeTrue();
        value.Should().BeSameAs(manifest);
    }

    [Test]
    public void SubjectScoping_ByParent_DoesNotCollide()
    {
        _index.StoreManifestBySubject("Config", "ParentA", new { Value = "a" });
        _index.StoreManifestBySubject("Config", "ParentB", new { Value = "b" });

        _index.TryResolveBySubject("Config", "ParentA", "Value", out var a).Should().BeTrue();
        _index.TryResolveBySubject("Config", "ParentB", "Value", out var b).Should().BeTrue();
        a.Should().Be("a");
        b.Should().Be("b");

        // A root-scoped lookup for the same subject must not resolve either parent-scoped entry.
        _index.HasManifestBySubject("Config", null).Should().BeFalse();
    }

    [Test]
    public void SubjectKey_DoesNotCollideWithLegacyTypeNameKey()
    {
        _index.StoreManifest("TypeA", "ResourceA", new { Value = "legacy" });
        _index.StoreManifestBySubject("ResourceA", "TypeA", new { Value = "subject" });

        _index.TryResolveProperty("TypeA", "ResourceA", "Value", out var legacy).Should().BeTrue();
        _index.TryResolveBySubject("ResourceA", "TypeA", "Value", out var subject).Should().BeTrue();
        legacy.Should().Be("legacy");
        subject.Should().Be("subject");
    }

    [Test]
    public void EnvironmentScope_IsolatesSubjects()
    {
        _index.BeginEnvironmentScope("Env1");
        _index.StoreManifestBySubject("S", null, new { Value = "one" });

        _index.BeginEnvironmentScope("Env2");
        _index.HasManifestBySubject("S", null).Should().BeFalse();

        _index.BeginEnvironmentScope("Env1");
        _index.TryResolveBySubject("S", null, "Value", out var value).Should().BeTrue();
        value.Should().Be("one");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ManifestReference constructor
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public void Constructor_NullOrWhitespaceSubject_Throws()
    {
        var actNull = () => new ManifestReference(null!);
        var actWhitespace = () => new ManifestReference("   ");

        actNull.Should().Throw<ArgumentException>();
        actWhitespace.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_SetsSubjectAndParent()
    {
        var reference = new ManifestReference("BaseConfig", "Parent");

        reference.Subject.Should().Be("BaseConfig");
        reference.ParentManifest.Should().Be("Parent");
        reference.PropertyPath.Should().BeEmpty();
    }

    [Test]
    public void SubjectReference_ResolvesWholeManifestByDefault()
    {
        var manifest = new { DependsOn = "[Type]R1" };
        _index.StoreManifestBySubject("BaseConfig", null, manifest);

        var reference = new ManifestReference("BaseConfig");

        InvokeTryResolve(reference, _index, out var value).Should().BeTrue();
        value.Should().BeSameAs(manifest);
    }

    [Test]
    public void SubjectReference_WithPropertyPath_ResolvesNestedValue()
    {
        _index.StoreManifestBySubject("BaseConfig", null, new { DependsOn = "[Type]R1" });

        var reference = new ManifestReference("BaseConfig") { PropertyPath = "DependsOn" };

        InvokeTryResolve(reference, _index, out var value).Should().BeTrue();
        value.Should().Be("[Type]R1");
    }

    [Test]
    public void GenericSubjectReference_BySubject_MapsTypedValue()
    {
        _index.StoreManifestBySubject("BaseConfig", null, new SampleManifest { DependsOn = "[Type]R1" });

        var reference = ManifestReference<SampleManifest>.BySubject(
            "BaseConfig",
            null,
            manifest => manifest.DependsOn);

        InvokeTryResolve(reference, _index, out var value).Should().BeTrue();
        value.Should().Be("[Type]R1");
    }

    private static bool InvokeTryResolve(ManifestReference reference, IManifestReferenceIndex index, out object? value)
    {
        var method = typeof(ManifestReference).GetMethod(
            "TryResolveValue",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        var args = new object?[] { index, null };
        var result = (bool)method.Invoke(reference, args)!;
        value = args[1];
        return result;
    }

    private sealed class SampleManifest : ManifestBase
    {
        public required string DependsOn { get; init; }
    }
}
