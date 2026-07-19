namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// The resolution target for a <c>ManifestReference</c> relative to the requesting
/// <see cref="IGenerationScope"/> (Manifest v2 phase P4, gap G9).
/// </summary>
public enum ManifestReferenceTarget
{
    /// <summary>Resolve within the requesting scope itself. Preserves v1 behavior exactly.</summary>
    ThisScope = 0,

    /// <summary>
    /// Resolve against a provider-defined "paired" scope coordinate (e.g. a BCDR failover
    /// region/data-center pair). Providers that support pairing must register the paired
    /// coordinate and implement <see cref="ITranslatableScope"/>; the base framework has no
    /// built-in notion of which coordinate is "paired" for a given provider.
    /// </summary>
    PairedScope = 1,
}
