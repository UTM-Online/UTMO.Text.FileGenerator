using FluentAssertions;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ManifestReferenceInfo{TManifest}"/> (Manifest v2 phase P1),
/// covering typed resolution, dangling-subject validation, and type-mismatch validation.
/// </summary>
[TestFixture]
public class ManifestReferenceInfoTests
{
    private ManifestReferenceIndex _index = null!;
    private LocalManifestProvider _provider = null!;
    private IGenerationScope _scope = null!;

    [SetUp]
    public void SetUp()
    {
        _index = new ManifestReferenceIndex();
        _provider = new LocalManifestProvider(_index);
        _scope = GenerationScope.ForEnvironment("Production");

        // Ensure no observation sink is registered unless a test opts in.
        ManifestReferenceInfo.ObservationSink = null;
    }

    [TearDown]
    public void TearDown() => ManifestReferenceInfo.ObservationSink = null;

    [Test]
    public void GetTypedManifest_WhenSubjectStoredWithMatchingType_ReturnsTypedInstance()
    {
        _provider.StoreManifestBySubject(_scope, "BaseConfig", null, new TestManifest { Value = "hello" });

        var referenceInfo = new ManifestReferenceInfo<TestManifest>(_provider, _scope, "BaseConfig");

        var manifest = referenceInfo.GetTypedManifest();

        manifest.Should().NotBeNull();
        manifest!.Value.Should().Be("hello");
    }

    [Test]
    public void GetTypedManifest_WhenSubjectMissing_ReturnsNull()
    {
        var referenceInfo = new ManifestReferenceInfo<TestManifest>(_provider, _scope, "DoesNotExist");

        referenceInfo.GetTypedManifest().Should().BeNull();
    }

    [Test]
    public void GetTypedManifest_WhenStoredTypeMismatches_ReturnsNull()
    {
        _provider.StoreManifestBySubject(_scope, "BaseConfig", null, new OtherManifest());

        var referenceInfo = new ManifestReferenceInfo<TestManifest>(_provider, _scope, "BaseConfig");

        referenceInfo.GetTypedManifest().Should().BeNull();
    }

    [Test]
    public void ValidateNoThrow_WhenDangling_ReturnsSingleError()
    {
        var referenceInfo = new ManifestReferenceInfo<TestManifest>(_provider, _scope, "Missing");

        var errors = referenceInfo.ValidateNoThrow().ToList();

        errors.Should().ContainSingle();
        errors[0].Message.Should().Contain("Dangling");
    }

    [Test]
    public void ValidateNoThrow_WhenTypeMismatches_ReturnsTypeMismatchError()
    {
        _provider.StoreManifestBySubject(_scope, "BaseConfig", null, new OtherManifest());
        var referenceInfo = new ManifestReferenceInfo<TestManifest>(_provider, _scope, "BaseConfig");

        var errors = referenceInfo.ValidateNoThrow().ToList();

        errors.Should().ContainSingle();
        errors[0].Message.Should().Contain("type mismatch");
    }

    [Test]
    public void ValidateNoThrow_WhenResolvable_ReturnsNoErrors()
    {
        _provider.StoreManifestBySubject(_scope, "BaseConfig", null, new TestManifest { Value = "ok" });
        var referenceInfo = new ManifestReferenceInfo<TestManifest>(_provider, _scope, "BaseConfig");

        referenceInfo.ValidateNoThrow().Should().BeEmpty();
    }

    [Test]
    public void GetTypedManifest_WhenObservationSinkRegistered_RecordsEdge()
    {
        _provider.StoreManifestBySubject(_scope, "BaseConfig", null, new TestManifest { Value = "ok" });
        var sink = new InMemoryManifestObservationSink();
        ManifestReferenceInfo.ObservationSink = sink;

        var referenceInfo = new ManifestReferenceInfo<TestManifest>(_provider, _scope, "BaseConfig", referrerIdentifier: "referrer-id");
        referenceInfo.GetTypedManifest();

        sink.Edges.Should().ContainSingle(e => e.ReferrerIdentifier == "referrer-id" && e.ReferencedIdentifier == referenceInfo.GetIdentifier());
    }

    [Test]
    public void GetIdentifier_IncludesProviderScopeTypeAndSubject()
    {
        var referenceInfo = new ManifestReferenceInfo<TestManifest>(_provider, _scope, "BaseConfig");

        var identifier = referenceInfo.GetIdentifier();

        identifier.Should().Contain("local").And.Contain("Production").And.Contain(nameof(TestManifest)).And.Contain("BaseConfig");
    }

    private sealed class TestManifest : IManifest
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class OtherManifest : IManifest
    {
    }
}
