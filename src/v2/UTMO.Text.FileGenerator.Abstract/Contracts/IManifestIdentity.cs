namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Optional generalized identity surface for a manifest payload (Manifest v2, phase P1, see
/// issue #57 / the <c>Manifests.v2</c> wiki design doc, §4.2). Manifest models are not required
/// to implement this interface — the framework continues to accept any <see cref="IManifest"/>
/// payload — but providers that want first-class identity (used by
/// <see cref="ManifestReferenceInfo{TManifest}"/>, portable Manifest Package emission, and
/// dependency-ordering observation) should implement it on their manifest base classes
/// (e.g. <c>ArmResourceManifestBase</c>, <c>DscConfigurationManifestBase</c>).
/// </summary>
public interface IManifestIdentity : IManifest
{
    /// <summary>The owning provider's kind, e.g. <c>"arm"</c>, <c>"dsc"</c>, <c>"local"</c>.</summary>
    string ProviderKind { get; }

    /// <summary>The stable subject identity (mirrors <see cref="IManifestProducer.ManifestSubject"/>).</summary>
    string Subject { get; }

    /// <summary>The optional parent manifest subject that scopes <see cref="Subject"/>.</summary>
    string? ParentSubject { get; }

    /// <summary>The rendered instance name of the manifest's source resource.</summary>
    string Name { get; }

    /// <summary>
    /// Returns a provider-defined stable address for this manifest (e.g. an ARM resource id, a
    /// DSC configuration path). Used for diagnostics and cross-provider addressing; the core
    /// framework does not interpret the returned value.
    /// </summary>
    string GetAddress();
}
