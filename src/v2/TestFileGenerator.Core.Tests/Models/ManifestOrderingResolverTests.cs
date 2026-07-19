using FluentAssertions;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Models;

namespace TestFileGenerator.Core.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ManifestOrderingResolver"/> (Manifest v2 phase P4, gap G8):
/// topological ordering from observed/contributed dependency edges, including cycle detection.
/// </summary>
[TestFixture]
public class ManifestOrderingResolverTests
{
    [Test]
    public void ComputeOrder_WithLinearChain_OrdersDependenciesFirst()
    {
        var edges = new[]
        {
            new ManifestOrderingEdge("B", "A"), // B depends on A: A must come before B
            new ManifestOrderingEdge("C", "B"), // C depends on B
        };

        var order = ManifestOrderingResolver.ComputeOrder(edges);

        order.Should().ContainInOrder("A", "B", "C");
    }

    [Test]
    public void ComputeOrder_WithDiamondDependency_ProducesValidOrder()
    {
        // D depends on B and C; both B and C depend on A.
        var edges = new[]
        {
            new ManifestOrderingEdge("B", "A"),
            new ManifestOrderingEdge("C", "A"),
            new ManifestOrderingEdge("D", "B"),
            new ManifestOrderingEdge("D", "C"),
        };

        var order = ManifestOrderingResolver.ComputeOrder(edges).ToList();

        order.Should().HaveCount(4);
        order.IndexOf("A").Should().BeLessThan(order.IndexOf("B"));
        order.IndexOf("A").Should().BeLessThan(order.IndexOf("C"));
        order.IndexOf("B").Should().BeLessThan(order.IndexOf("D"));
        order.IndexOf("C").Should().BeLessThan(order.IndexOf("D"));
    }

    [Test]
    public void ComputeOrder_WithCycle_Throws()
    {
        var edges = new[]
        {
            new ManifestOrderingEdge("A", "B"),
            new ManifestOrderingEdge("B", "A"),
        };

        var act = () => ManifestOrderingResolver.ComputeOrder(edges);

        act.Should().Throw<InvalidOperationException>().WithMessage("*cycle*");
    }

    [Test]
    public void ComputeOrder_WithNoEdges_ReturnsEmpty()
    {
        var order = ManifestOrderingResolver.ComputeOrder(Array.Empty<ManifestOrderingEdge>());

        order.Should().BeEmpty();
    }

    [Test]
    public void ComputeOrder_FromSinkAndContributors_MergesEdges()
    {
        var sink = new InMemoryManifestObservationSink();
        sink.OnResolved(new ManifestOrderingEdge("B", "A"));

        var contributor = new StaticContributor(new ManifestOrderingEdge("C", "B"));

        var order = ManifestOrderingResolver.ComputeOrder(sink, new[] { contributor });

        order.Should().ContainInOrder("A", "B", "C");
    }

    private sealed class StaticContributor : IManifestOrderingContributor
    {
        private readonly ManifestOrderingEdge _edge;

        public StaticContributor(ManifestOrderingEdge edge) => _edge = edge;

        public IEnumerable<ManifestOrderingEdge> GetEdges()
        {
            yield return _edge;
        }
    }
}
