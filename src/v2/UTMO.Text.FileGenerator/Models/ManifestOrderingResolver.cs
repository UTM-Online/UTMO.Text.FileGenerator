namespace UTMO.Text.FileGenerator.Models;

using UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Computes a canonical topological ordering from observed manifest dependency edges plus any
/// provider-contributed supplemental edges (Manifest v2 phase P4, gap G8). The base framework
/// owns this computation; providers only contribute edges via
/// <see cref="IManifestOrderingContributor"/> or by having their references observed through
/// <see cref="IManifestObservationSink"/>.
/// </summary>
/// <remarks>
/// Uses Kahn's algorithm. Nodes that never appear as an edge endpoint are not included in the
/// result — callers that need a total order over "all manifests" (not just those with declared
/// dependencies) should union this order with the full manifest identifier set and treat
/// unordered manifests as free to run first.
/// </remarks>
public static class ManifestOrderingResolver
{
    /// <summary>
    /// Computes a topological order over all node identifiers referenced by <paramref name="edges"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the edges contain a cycle.</exception>
    public static IReadOnlyList<string> ComputeOrder(IEnumerable<ManifestOrderingEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(edges);

        // dependents[x] = manifests that depend on x (x must come before them)
        var dependents = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var inDegree = new Dictionary<string, int>(StringComparer.Ordinal);

        void EnsureNode(string id)
        {
            if (!dependents.ContainsKey(id))
            {
                dependents[id] = new List<string>();
            }

            inDegree.TryAdd(id, 0);
        }

        foreach (var edge in edges)
        {
            EnsureNode(edge.ReferrerIdentifier);
            EnsureNode(edge.ReferencedIdentifier);

            // ReferencedIdentifier must be produced before ReferrerIdentifier.
            dependents[edge.ReferencedIdentifier].Add(edge.ReferrerIdentifier);
            inDegree[edge.ReferrerIdentifier]++;
        }

        var ready = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key).OrderBy(k => k, StringComparer.Ordinal));
        var order = new List<string>(inDegree.Count);

        while (ready.Count > 0)
        {
            var node = ready.Dequeue();
            order.Add(node);

            foreach (var dependent in dependents[node].OrderBy(d => d, StringComparer.Ordinal))
            {
                inDegree[dependent]--;

                if (inDegree[dependent] == 0)
                {
                    ready.Enqueue(dependent);
                }
            }
        }

        if (order.Count != inDegree.Count)
        {
            var cyclic = inDegree.Keys.Except(order).OrderBy(k => k, StringComparer.Ordinal);
            throw new InvalidOperationException(
                $"Manifest ordering failed: a dependency cycle was detected involving: {string.Join(", ", cyclic)}.");
        }

        return order;
    }

    /// <summary>
    /// Computes the order from an <see cref="InMemoryManifestObservationSink"/> plus any
    /// registered <see cref="IManifestOrderingContributor"/>s.
    /// </summary>
    public static IReadOnlyList<string> ComputeOrder(
        InMemoryManifestObservationSink sink,
        IEnumerable<IManifestOrderingContributor>? contributors = null)
    {
        ArgumentNullException.ThrowIfNull(sink);

        var edges = sink.Edges.AsEnumerable();

        if (contributors is not null)
        {
            edges = edges.Concat(contributors.SelectMany(c => c.GetEdges()));
        }

        return ComputeOrder(edges);
    }
}
