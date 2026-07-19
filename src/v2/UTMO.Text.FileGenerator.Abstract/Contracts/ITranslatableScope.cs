namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Optional translation seam (Manifest v2 phase P4, gap G9) that lets an
/// <see cref="IManifestProvider"/> map a foreign provider's environment/coordinate aliases onto
/// the local <see cref="IGenerationScope"/> before lookup. Implement this on a provider when its
/// scope vocabulary differs from the requesting scope's (e.g. an external Manifest Package uses
/// <c>"Region"</c> where the local scope uses <c>"DataCenter"</c>).
/// </summary>
/// <remarks>
/// Providers that do not implement this interface are treated as requiring no translation — the
/// requesting scope is used as-is. This preserves v1/byte-for-byte behavior for the default
/// <c>LocalManifestProvider</c>.
/// </remarks>
public interface ITranslatableScope
{
    /// <summary>
    /// Translates <paramref name="requestedScope"/> into the scope this provider expects, or
    /// returns <paramref name="requestedScope"/> unchanged when no translation is necessary.
    /// </summary>
    IGenerationScope Translate(IGenerationScope requestedScope);
}
