namespace UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Declares a single Generation Scope coordinate dimension and the provider that owns it
/// (Manifest v2 phase P4, gap G2). Registration is declaration-only: name, owning provider kind,
/// and whether the coordinate is required. Value schemas/validators remain provider-owned.
/// </summary>
/// <param name="Name">The coordinate dimension name (e.g. <c>"DataCenter"</c>). Matched case-insensitively.</param>
/// <param name="OwnerProviderKind">The provider kind that owns/declares this coordinate (e.g. <c>"arm"</c>).</param>
/// <param name="Required">Whether a scope built for <paramref name="OwnerProviderKind"/> must supply this coordinate.</param>
public sealed record GenerationScopeCoordinate(string Name, string OwnerProviderKind, bool Required = false);

/// <summary>
/// Base registry for provider-declared Generation Scope coordinates (Manifest v2 phase P4, gap
/// G2). <see cref="Environment"/> is pre-registered by the base framework. Registering the same
/// coordinate <see cref="GenerationScopeCoordinate.Name"/> under two different owners is a
/// fail-fast configuration error, surfaced at registration time (typically application startup)
/// rather than at scope-build or resolution time.
/// </summary>
public interface IGenerationScopeCoordinateRegistry
{
    /// <summary>
    /// Registers <paramref name="coordinate"/>. Throws
    /// <see cref="InvalidOperationException"/> if a coordinate with the same
    /// <see cref="GenerationScopeCoordinate.Name"/> is already registered under a different
    /// <see cref="GenerationScopeCoordinate.OwnerProviderKind"/>. Re-registering an identical
    /// coordinate (same name and owner) is a no-op.
    /// </summary>
    void Register(GenerationScopeCoordinate coordinate);

    /// <summary>Returns whether a coordinate with the given name has been registered.</summary>
    bool IsRegistered(string name);

    /// <summary>Attempts to retrieve the registered coordinate declaration for <paramref name="name"/>.</summary>
    bool TryGet(string name, out GenerationScopeCoordinate? coordinate);

    /// <summary>All coordinates registered so far, including the pre-registered <c>"Environment"</c> coordinate.</summary>
    IReadOnlyCollection<GenerationScopeCoordinate> All { get; }
}
