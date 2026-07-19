using FluentAssertions;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="GenerationScopeCoordinateRegistry"/> (Manifest v2 phase P4, gap G2):
/// declaration-only coordinate registration with fail-fast conflict detection.
/// </summary>
[TestFixture]
public class GenerationScopeCoordinateRegistryTests
{
    private GenerationScopeCoordinateRegistry _registry = null!;

    [SetUp]
    public void SetUp() => _registry = new GenerationScopeCoordinateRegistry();

    [Test]
    public void Environment_IsPreRegistered()
    {
        _registry.IsRegistered("Environment").Should().BeTrue();
        _registry.TryGet("Environment", out var coordinate).Should().BeTrue();
        coordinate!.Required.Should().BeTrue();
    }

    [Test]
    public void Register_NewCoordinate_Succeeds()
    {
        _registry.Register(new GenerationScopeCoordinate("DataCenter", "arm"));

        _registry.IsRegistered("DataCenter").Should().BeTrue();
        _registry.TryGet("DataCenter", out var coordinate).Should().BeTrue();
        coordinate!.OwnerProviderKind.Should().Be("arm");
    }

    [Test]
    public void Register_SameCoordinateSameOwnerTwice_IsIdempotent()
    {
        _registry.Register(new GenerationScopeCoordinate("DataCenter", "arm"));
        var act = () => _registry.Register(new GenerationScopeCoordinate("DataCenter", "arm"));

        act.Should().NotThrow();
    }

    [Test]
    public void Register_SameCoordinateDifferentOwner_ThrowsFailFast()
    {
        _registry.Register(new GenerationScopeCoordinate("DataCenter", "arm"));
        var act = () => _registry.Register(new GenerationScopeCoordinate("DataCenter", "dsc"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Register_EnvironmentUnderDifferentOwner_ThrowsFailFast()
    {
        var act = () => _registry.Register(new GenerationScopeCoordinate("Environment", "arm"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void All_IncludesPreRegisteredAndAddedCoordinates()
    {
        _registry.Register(new GenerationScopeCoordinate("DataCenter", "arm"));

        _registry.All.Select(c => c.Name).Should().Contain(new[] { "Environment", "DataCenter" });
    }
}
