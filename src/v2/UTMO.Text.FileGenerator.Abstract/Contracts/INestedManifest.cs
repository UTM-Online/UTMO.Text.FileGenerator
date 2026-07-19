namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Typed nesting contract (Manifest v2 phase P4, gap G7): a manifest that is scoped beneath a
/// parent manifest can expose that parent in a strongly typed way, mirroring ConfigGen's
/// subjects-chain nesting.
/// </summary>
/// <typeparam name="TParent">The parent manifest's identity type.</typeparam>
public interface INestedManifest<out TParent> : IManifest
    where TParent : class, IManifest
{
    /// <summary>Returns this manifest's parent. May resolve lazily via the owning provider.</summary>
    TParent GetParentManifest();
}
