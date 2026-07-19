namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// A provider-agnostic coordinate set that a <c>ManifestReference</c> resolves against.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Environment"/> is the mandatory dimension and preserves v1 behavior exactly: the
/// manifest index has always been environment-scoped. Additional dimensions are opt-in via
/// <see cref="Coordinates"/> — e.g. an ARM provider may register a <c>"DataCenter"</c>
/// coordinate, DSC may register <c>"Node"</c>. The core framework never hard-codes these names;
/// it only guarantees that <see cref="Environment"/> is always present.
/// </para>
/// <para>
/// A scope with no additional coordinates reproduces v1's single-axis (environment-only)
/// resolution byte-for-byte.
/// </para>
/// </remarks>
public interface IGenerationScope
{
    /// <summary>The mandatory environment dimension of this scope.</summary>
    string Environment { get; }

    /// <summary>
    /// Additional provider-declared coordinates that further qualify this scope (e.g.
    /// <c>{ "DataCenter", "EUS" }</c>). Empty for a v1-parity, environment-only scope.
    /// </summary>
    IReadOnlyDictionary<string, string> Coordinates { get; }

    /// <summary>
    /// Attempts to read the value of the named <paramref name="dimension"/> from this scope.
    /// <see cref="Environment"/> is always resolvable via the dimension name <c>"Environment"</c>
    /// in addition to <see cref="Coordinates"/> lookups.
    /// </summary>
    bool TryGetCoordinate(string dimension, out string? value);

    /// <summary>
    /// Returns a stable, human-readable identifier for this scope, suitable for use as part of
    /// a cache/index key (e.g. <c>"env"</c> or <c>"env/DataCenter=EUS"</c>).
    /// </summary>
    string GetIdentifier();
}
