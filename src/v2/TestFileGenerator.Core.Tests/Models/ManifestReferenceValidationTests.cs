using FluentAssertions;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ManifestReference.ValidateNoThrow"/> and the typed override on
/// <see cref="ManifestReference{TSourceManifest}"/> (Manifest v2 phase P2, gaps G3/G10).
/// </summary>
[TestFixture]
public class ManifestReferenceValidationTests
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
    }

    [Test]
    public void ValidateNoThrow_UntypedSubjectReference_WhenResolvable_ReturnsNoErrors()
    {
        _provider.StoreManifestBySubject(_scope, "BaseConfig", null, new { Value = "ok" });
        var reference = new ManifestReference("BaseConfig");

        reference.ValidateNoThrow(_provider, _scope).Should().BeEmpty();
    }

    [Test]
    public void ValidateNoThrow_UntypedSubjectReference_WhenDangling_ReturnsError()
    {
        var reference = new ManifestReference("Missing");

        reference.ValidateNoThrow(_provider, _scope).Should().ContainSingle();
    }

    [Test]
    public void ValidateNoThrow_LegacyReference_WhenDangling_ReturnsError()
    {
        var reference = new ManifestReference { ResourceTypeName = "TypeA", ResourceName = "ResourceA" };

        reference.ValidateNoThrow(_provider, _scope).Should().ContainSingle();
    }

    [Test]
    public void ValidateNoThrow_TypedReference_WhenTypeMatches_ReturnsNoErrors()
    {
        _provider.StoreManifestBySubject(_scope, "BaseConfig", null, new NetworkManifest { SubnetId = "subnet-1" });
        var reference = ManifestReference<NetworkManifest>.BySubject("BaseConfig", null, m => m.SubnetId);

        reference.ValidateNoThrow(_provider, _scope).Should().BeEmpty();
    }

    [Test]
    public void ValidateNoThrow_TypedReference_WhenTypeMismatches_ReturnsError()
    {
        _provider.StoreManifestBySubject(_scope, "BaseConfig", null, new OtherManifest());
        var reference = ManifestReference<NetworkManifest>.BySubject("BaseConfig", null, m => m.SubnetId);

        var errors = reference.ValidateNoThrow(_provider, _scope).ToList();

        errors.Should().ContainSingle();
        errors[0].Message.Should().Contain("type mismatch");
    }

    [Test]
    public void ValidateNoThrow_TypedReference_WhenDangling_ReturnsError()
    {
        var reference = ManifestReference<NetworkManifest>.BySubject("Missing", null, m => m.SubnetId);

        reference.ValidateNoThrow(_provider, _scope).Should().ContainSingle();
    }

    private sealed class NetworkManifest : IManifest
    {
        public string SubnetId { get; init; } = string.Empty;
    }

    private sealed class OtherManifest : IManifest
    {
    }
}
