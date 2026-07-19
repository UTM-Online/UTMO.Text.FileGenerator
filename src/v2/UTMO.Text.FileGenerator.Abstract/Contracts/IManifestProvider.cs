namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Provider-agnostic abstraction over manifest storage and lookup, scoped by
/// <see cref="IGenerationScope"/> rather than the ambient environment scope used internally by
/// <c>IManifestReferenceIndex</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the "local" provider seam described by the Manifest v2 design (see the
/// <c>Manifests.v2</c> wiki page and issue #57, phase P0): <see cref="IManifestProvider"/>
/// wraps today's in-memory, environment-scoped index so future providers (e.g. an
/// artifact-backed provider that reads a published Manifest Package) can be substituted without
/// changing reference-resolution call sites.
/// </para>
/// <para>
/// The member surface intentionally mirrors <c>IManifestReferenceIndex</c>'s existing
/// capabilities (subject-based and legacy type/name lookups) so the default implementation can be
/// a thin, behavior-preserving adapter. Typed manifest identity (<c>IManifest</c>-based)
/// resolution lands in a later phase (P1) as <c>ManifestReferenceInfo&lt;T&gt;</c>.
/// </para>
/// </remarks>
public interface IManifestProvider
{
    /// <summary>Stores the manifest data for the given resource within <paramref name="scope"/>.</summary>
    void StoreManifest(IGenerationScope scope, string resourceTypeName, string resourceName, object? manifestData);

    /// <summary>
    /// Attempts to navigate <paramref name="propertyPath"/> inside the manifest data previously
    /// stored for <c>(resourceTypeName, resourceName)</c> within <paramref name="scope"/>.
    /// </summary>
    bool TryResolveProperty(IGenerationScope scope, string resourceTypeName, string resourceName, string propertyPath, out object? value);

    /// <summary>Returns whether a manifest has been stored for the given resource within <paramref name="scope"/>.</summary>
    bool HasManifest(IGenerationScope scope, string resourceTypeName, string resourceName);

    /// <summary>
    /// Stores the manifest data for the given <paramref name="subject"/> (optionally scoped by
    /// <paramref name="parentManifest"/>) within <paramref name="scope"/>.
    /// </summary>
    void StoreManifestBySubject(IGenerationScope scope, string subject, string? parentManifest, object? manifestData);

    /// <summary>
    /// Attempts to navigate <paramref name="propertyPath"/> inside the manifest data stored for
    /// <paramref name="subject"/> (optionally scoped by <paramref name="parentManifest"/>) within
    /// <paramref name="scope"/>. An empty <paramref name="propertyPath"/> resolves the entire
    /// manifest object.
    /// </summary>
    bool TryResolveBySubject(IGenerationScope scope, string subject, string? parentManifest, string propertyPath, out object? value);

    /// <summary>Returns whether a manifest has been stored for the given subject/parent within <paramref name="scope"/>.</summary>
    bool HasManifestBySubject(IGenerationScope scope, string subject, string? parentManifest);
}
