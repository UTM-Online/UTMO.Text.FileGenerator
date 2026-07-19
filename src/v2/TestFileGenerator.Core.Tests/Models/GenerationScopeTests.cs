using FluentAssertions;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="GenerationScope"/>.
/// </summary>
[TestFixture]
public class GenerationScopeTests
{
    [Test]
    public void Constructor_WithNullOrWhitespaceEnvironment_Throws()
    {
        var act = () => new GenerationScope("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void ForEnvironment_CreatesScope_WithNoCoordinates()
    {
        var scope = GenerationScope.ForEnvironment("Production");

        scope.Environment.Should().Be("Production");
        scope.Coordinates.Should().BeEmpty();
    }

    [Test]
    public void GetIdentifier_WithNoCoordinates_ReturnsEnvironmentOnly()
    {
        var scope = GenerationScope.ForEnvironment("Production");
        scope.GetIdentifier().Should().Be("Production");
    }

    [Test]
    public void GetIdentifier_WithCoordinates_IsStableAndOrdered()
    {
        var scope = new GenerationScope(
            "Production",
            new Dictionary<string, string> { ["Region"] = "West", ["DataCenter"] = "EUS" });

        // Coordinates should be ordered deterministically regardless of insertion order.
        scope.GetIdentifier().Should().Be("Production/DataCenter=EUS/Region=West");
    }

    [Test]
    public void TryGetCoordinate_ForEnvironmentDimension_ReturnsEnvironmentValue()
    {
        var scope = GenerationScope.ForEnvironment("Production");

        scope.TryGetCoordinate("Environment", out var value).Should().BeTrue();
        value.Should().Be("Production");
    }

    [Test]
    public void TryGetCoordinate_ForDeclaredCoordinate_ReturnsValue()
    {
        var scope = new GenerationScope("Production", new Dictionary<string, string> { ["DataCenter"] = "EUS" });

        scope.TryGetCoordinate("DataCenter", out var value).Should().BeTrue();
        value.Should().Be("EUS");
    }

    [Test]
    public void TryGetCoordinate_ForDeclaredCoordinate_IsCaseInsensitive()
    {
        var scope = new GenerationScope("Production", new Dictionary<string, string> { ["DataCenter"] = "EUS" });

        scope.TryGetCoordinate("datacenter", out var value).Should().BeTrue();
        value.Should().Be("EUS");
    }

    [Test]
    public void TryGetCoordinate_ForUnknownDimension_ReturnsFalse()
    {
        var scope = GenerationScope.ForEnvironment("Production");

        scope.TryGetCoordinate("DataCenter", out var value).Should().BeFalse();
        value.Should().BeNull();
    }

    [Test]
    public void ToString_MatchesGetIdentifier()
    {
        var scope = new GenerationScope("Production", new Dictionary<string, string> { ["DataCenter"] = "EUS" });
        scope.ToString().Should().Be(scope.GetIdentifier());
    }
}
