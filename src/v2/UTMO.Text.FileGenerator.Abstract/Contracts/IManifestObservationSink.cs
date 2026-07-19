namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// A directed dependency edge between two manifests, identified by their stable identifiers
/// (see <see cref="ManifestReferenceInfo.GetIdentifier"/> / <see cref="IManifestIdentity"/>).
/// Manifest v2 phase P4, gap G8.
/// </summary>
/// <param name="ReferrerIdentifier">The manifest that declares/holds the reference.</param>
/// <param name="ReferencedIdentifier">The manifest being referenced (must be produced before <paramref name="ReferrerIdentifier"/>).</param>
public readonly record struct ManifestOrderingEdge(string ReferrerIdentifier, string ReferencedIdentifier);

/// <summary>
/// Records dependency edges observed during manifest reference resolution (Manifest v2 phase P4,
/// gap G8). The base framework calls <see cref="OnResolved"/> every time
/// <see cref="ManifestReferenceInfo.GetManifest"/> successfully resolves a reference, allowing
/// providers/consumers to derive a deployment-ordering graph without any manual bookkeeping.
/// </summary>
public interface IManifestObservationSink
{
    /// <summary>Records that <paramref name="edge"/> was observed during resolution.</summary>
    void OnResolved(ManifestOrderingEdge edge);
}

/// <summary>
/// Optional, provider-owned supplemental ordering contributor (Manifest v2 phase P4, gap G8).
/// Automatic reference-derived edges (via <see cref="IManifestObservationSink"/>) are the primary
/// ordering mechanism; a provider may additionally contribute declarative edges for constraints
/// that are not expressed as manifest references (e.g. "always deploy the network before any
/// compute resource").
/// </summary>
public interface IManifestOrderingContributor
{
    /// <summary>Returns supplemental ordering edges to add to the dependency graph.</summary>
    IEnumerable<ManifestOrderingEdge> GetEdges();
}
