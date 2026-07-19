namespace UTMO.Text.FileGenerator.Models;

using System.Collections.Concurrent;
using UTMO.Text.FileGenerator.Abstract.Contracts;

/// <inheritdoc cref="IGenerationScopeCoordinateRegistry"/>
/// <remarks>
/// Pre-registers the mandatory <c>"Environment"</c> coordinate under owner kind <c>"core"</c>.
/// Registered as a singleton so provider packages can register their coordinates once at
/// startup (Manifest v2 phase P4, gap G2) and have registration conflicts fail fast rather than
/// surface as confusing scope-resolution errors later.
/// </remarks>
public sealed class GenerationScopeCoordinateRegistry : IGenerationScopeCoordinateRegistry
{
    public const string CoreOwnerKind = "core";
    public const string EnvironmentCoordinateName = "Environment";

    private readonly ConcurrentDictionary<string, GenerationScopeCoordinate> _coordinates =
        new(StringComparer.OrdinalIgnoreCase);

    public GenerationScopeCoordinateRegistry()
    {
        // Pre-register the mandatory Environment dimension; every IGenerationScope already
        // carries it, so it must always be considered "known".
        this._coordinates[EnvironmentCoordinateName] =
            new GenerationScopeCoordinate(EnvironmentCoordinateName, CoreOwnerKind, Required: true);
    }

    /// <inheritdoc/>
    public void Register(GenerationScopeCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        ArgumentException.ThrowIfNullOrWhiteSpace(coordinate.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(coordinate.OwnerProviderKind);

        this._coordinates.AddOrUpdate(
            coordinate.Name,
            coordinate,
            (_, existing) =>
            {
                if (!string.Equals(existing.OwnerProviderKind, coordinate.OwnerProviderKind, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Generation Scope coordinate '{coordinate.Name}' is already registered to provider " +
                        $"'{existing.OwnerProviderKind}' and cannot be re-registered to provider " +
                        $"'{coordinate.OwnerProviderKind}'. Each coordinate name must be owned by exactly one provider.");
                }

                // Same name + same owner: allow idempotent re-registration, honoring the more
                // restrictive Required flag if the two declarations disagree.
                return existing with { Required = existing.Required || coordinate.Required };
            });
    }

    /// <inheritdoc/>
    public bool IsRegistered(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return this._coordinates.ContainsKey(name);
    }

    /// <inheritdoc/>
    public bool TryGet(string name, out GenerationScopeCoordinate? coordinate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return this._coordinates.TryGetValue(name, out coordinate);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<GenerationScopeCoordinate> All => this._coordinates.Values.ToArray();
}
