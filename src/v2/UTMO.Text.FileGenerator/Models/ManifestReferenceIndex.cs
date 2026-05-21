namespace UTMO.Text.FileGenerator.Models;

using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// Stores and retrieves in-memory manifest data indexed by resource type name and resource
/// name. The index is populated by <c>ManifestIndexBuildingPlugin</c> before template
/// rendering and consumed by <c>ManifestReferenceResolverPlugin</c> during the per-resource
/// before-render phase.
/// </summary>
public interface IManifestReferenceIndex
{
    /// <summary>
    /// Sets the environment name that scopes all subsequent <see cref="StoreManifest"/>,
    /// <see cref="TryResolveProperty"/> and <see cref="HasManifest"/> calls within the
    /// current async execution context.  Call this before building or resolving manifests
    /// for a specific environment so that data from different environments cannot collide.
    /// </summary>
    void BeginEnvironmentScope(string environmentName);

    /// <summary>
    /// Stores the manifest data for the given resource within the current environment scope.
    /// Overwrites any previously stored value for the same resource key within the same scope.
    /// </summary>
    void StoreManifest(string resourceTypeName, string resourceName, object? manifestData);

    /// <summary>
    /// Attempts to navigate <paramref name="propertyPath"/> (a dot-separated path) inside
    /// the manifest data that was previously stored for
    /// <c>(resourceTypeName, resourceName)</c> within the current environment scope.
    /// </summary>
    /// <param name="resourceTypeName">The resource type name used as part of the lookup key.</param>
    /// <param name="resourceName">The resource name used as part of the lookup key.</param>
    /// <param name="propertyPath">A dot-separated property path, e.g. <c>"DependsOn"</c> or <c>"Network.SubnetId"</c>.</param>
    /// <param name="value">The resolved value when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the property was found; otherwise <see langword="false"/>.</returns>
    bool TryResolveProperty(string resourceTypeName, string resourceName, string propertyPath, out object? value);

    /// <summary>Returns whether a manifest has been stored for the given resource within the current environment scope.</summary>
    bool HasManifest(string resourceTypeName, string resourceName);

    /// <summary>Removes all stored manifests.  Used to reset the index between runs.</summary>
    void Clear();
}

/// <inheritdoc cref="IManifestReferenceIndex"/>
public sealed class ManifestReferenceIndex : IManifestReferenceIndex
{
    private readonly ConcurrentDictionary<string, object?> _data =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tracks the currently active environment scope for the async execution context.
    /// Set via <see cref="BeginEnvironmentScope"/> before building or resolving manifests
    /// for a specific environment.
    /// </summary>
    private readonly AsyncLocal<string?> _currentEnvironment = new();

    /// <summary>
    /// Cache of <see cref="PropertyInfo"/> results keyed by <c>TypeFullName:PropertyName</c>.
    /// Avoids repeated reflection calls for the same type/property combination.
    /// </summary>
    private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertyCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public void BeginEnvironmentScope(string environmentName) =>
        _currentEnvironment.Value = environmentName;

    private string MakeKey(string resourceTypeName, string resourceName) =>
        _currentEnvironment.Value is { } env
            ? $"{env}/{resourceTypeName}/{resourceName}"
            : $"{resourceTypeName}/{resourceName}";

    /// <inheritdoc/>
    public void StoreManifest(string resourceTypeName, string resourceName, object? manifestData) =>
        _data[MakeKey(resourceTypeName, resourceName)] = manifestData;

    /// <inheritdoc/>
    public bool TryResolveProperty(
        string resourceTypeName,
        string resourceName,
        string propertyPath,
        out object? value)
    {
        var key = MakeKey(resourceTypeName, resourceName);

        if (!_data.TryGetValue(key, out var manifestData))
        {
            value = null;
            return false;
        }

        return TryNavigatePath(manifestData, propertyPath, out value);
    }

    /// <inheritdoc/>
    public bool HasManifest(string resourceTypeName, string resourceName) =>
        _data.ContainsKey(MakeKey(resourceTypeName, resourceName));

    /// <inheritdoc/>
    public void Clear() => _data.Clear();

    /// <summary>
    /// Traverses a dot-separated <paramref name="path"/> starting from <paramref name="data"/>.
    /// Supports both <see cref="IDictionary{TKey,TValue}"/> objects and plain CLR objects
    /// (resolved via reflection).
    /// </summary>
    private static bool TryNavigatePath(object? data, string path, out object? value)
    {
        if (string.IsNullOrEmpty(path))
        {
            value = data;
            return data != null;
        }

        var segments = path.Split('.');
        object? current = data;

        foreach (var segment in segments)
        {
            if (current is null)
            {
                value = null;
                return false;
            }

            if (TryGetValueFromDictionary(current, segment, out var dictValue))
            {
                current = dictValue;
                continue;
            }

            var cacheKey = $"{current.GetType().FullName}:{segment}";
            var prop = PropertyCache.GetOrAdd(
                cacheKey,
                _ => current.GetType()
                            .GetProperty(
                                segment,
                                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

            if (prop is null)
            {
                value = null;
                return false;
            }

            current = prop.GetValue(current);
        }

        value = current;
        return true;
    }

    private static bool TryGetValueFromDictionary(object obj, string key, out object? value)
    {
        // IDictionary<string, object?>
        if (obj is IDictionary<string, object?> typedDict)
        {
            return typedDict.TryGetValue(key, out value);
        }

        // IDictionary<string, object>
        if (obj is IDictionary<string, object> objectDict)
        {
            if (objectDict.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }

            value = null;
            return false;
        }

        value = null;
        return false;
    }
}
