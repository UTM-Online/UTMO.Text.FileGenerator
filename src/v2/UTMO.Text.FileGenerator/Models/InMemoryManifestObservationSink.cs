namespace UTMO.Text.FileGenerator.Models;

using UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Default in-memory <see cref="IManifestObservationSink"/> that simply records every observed
/// edge (Manifest v2 phase P4, gap G8). Thread-safe: concurrent renders may observe edges from
/// multiple resources simultaneously.
/// </summary>
public sealed class InMemoryManifestObservationSink : IManifestObservationSink
{
    private readonly System.Collections.Concurrent.ConcurrentBag<ManifestOrderingEdge> _edges = new();

    /// <inheritdoc/>
    public void OnResolved(ManifestOrderingEdge edge) => this._edges.Add(edge);

    /// <summary>All edges observed so far.</summary>
    public IReadOnlyCollection<ManifestOrderingEdge> Edges => this._edges.ToArray();

    /// <summary>Removes all recorded edges. Used to reset between runs/environments.</summary>
    public void Clear() => this._edges.Clear();
}
