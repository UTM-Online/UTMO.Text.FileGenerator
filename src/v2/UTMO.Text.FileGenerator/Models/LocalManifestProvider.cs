namespace UTMO.Text.FileGenerator.Models;

using UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Default <see cref="IManifestProvider"/> implementation that wraps the existing
/// <see cref="IManifestReferenceIndex"/>, translating an explicit <see cref="IGenerationScope"/>
/// into the index's ambient environment scope (<see cref="IManifestReferenceIndex.BeginEnvironmentScope"/>).
/// </summary>
/// <remarks>
/// This is the "local" manifest provider called out in the Manifest v2 design (issue #57, phase
/// P0): it is a behavior-preserving adapter — all storage/lookup mechanics remain exactly as they
/// were in v1, only the scoping input changes from ambient <c>AsyncLocal</c> state to an explicit
/// <see cref="IGenerationScope"/> parameter. Only the <see cref="IGenerationScope.Environment"/>
/// dimension is honored today; additional coordinates are reserved for future Generation Scope
/// phases (P4) and are currently ignored by this provider.
/// </remarks>
public sealed class LocalManifestProvider : IManifestProvider
{
    private readonly IManifestReferenceIndex _index;

    public LocalManifestProvider(IManifestReferenceIndex index)
    {
        this._index = index;
    }

    /// <inheritdoc/>
    public string ProviderKind => "local";

    /// <inheritdoc/>
    public void StoreManifest(IGenerationScope scope, string resourceTypeName, string resourceName, object? manifestData)
    {
        using var _ = this.BeginScope(scope);
        this._index.StoreManifest(resourceTypeName, resourceName, manifestData);
    }

    /// <inheritdoc/>
    public bool TryResolveProperty(IGenerationScope scope, string resourceTypeName, string resourceName, string propertyPath, out object? value)
    {
        using var _ = this.BeginScope(scope);
        return this._index.TryResolveProperty(resourceTypeName, resourceName, propertyPath, out value);
    }

    /// <inheritdoc/>
    public bool HasManifest(IGenerationScope scope, string resourceTypeName, string resourceName)
    {
        using var _ = this.BeginScope(scope);
        return this._index.HasManifest(resourceTypeName, resourceName);
    }

    /// <inheritdoc/>
    public void StoreManifestBySubject(IGenerationScope scope, string subject, string? parentManifest, object? manifestData)
    {
        using var _ = this.BeginScope(scope);
        this._index.StoreManifestBySubject(subject, parentManifest, manifestData);
    }

    /// <inheritdoc/>
    public bool TryResolveBySubject(IGenerationScope scope, string subject, string? parentManifest, string propertyPath, out object? value)
    {
        using var _ = this.BeginScope(scope);
        return this._index.TryResolveBySubject(subject, parentManifest, propertyPath, out value);
    }

    /// <inheritdoc/>
    public bool HasManifestBySubject(IGenerationScope scope, string subject, string? parentManifest)
    {
        using var _ = this.BeginScope(scope);
        return this._index.HasManifestBySubject(subject, parentManifest);
    }

    private IDisposable BeginScope(IGenerationScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope.Environment);
        return this._index.BeginEnvironmentScope(scope.Environment);
    }
}
