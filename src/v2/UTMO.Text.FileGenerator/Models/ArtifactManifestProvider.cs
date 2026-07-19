namespace UTMO.Text.FileGenerator.Models;

using Newtonsoft.Json.Linq;
using UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// A read-only <see cref="IManifestProvider"/> that resolves manifest references against a
/// published, portable **Manifest Package** (see <c>ManifestPackageEmissionPlugin</c> and the
/// <c>Manifests.v2</c> wiki design doc, §6) produced by *another* project's generation run
/// (Manifest v2 phase P3, gap G5/G4). This is what makes manifest references portable across
/// repositories: an ARM project can reference subjects from a DSC project's published package
/// with no source dependency on the DSC project.
/// </summary>
/// <remarks>
/// <para>
/// Construct with the directory containing a <c>manifest-package.json</c> file (typically the
/// <c>Manifests/</c> output folder of the producing project, or the contents of a restored
/// Manifest Package NuGet package). The package is parsed once, eagerly, at construction time.
/// </para>
/// <para>
/// Only the <see cref="IGenerationScope.Environment"/> dimension is matched against the
/// package's recorded environment; additional coordinates are reserved for future phases.
/// </para>
/// </remarks>
public sealed class ArtifactManifestProvider : IManifestProvider
{
    private readonly string _environment;
    private readonly IReadOnlyDictionary<string, JObject> _bySubjectKey;
    private readonly IReadOnlyDictionary<string, JObject> _byLegacyKey;

    /// <summary>
    /// Loads a Manifest Package from <paramref name="packageDirectory"/>.
    /// </summary>
    /// <param name="packageDirectory">The directory containing <c>manifest-package.json</c>.</param>
    /// <exception cref="FileNotFoundException">Thrown when <c>manifest-package.json</c> does not exist in <paramref name="packageDirectory"/>.</exception>
    public ArtifactManifestProvider(string packageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageDirectory);

        var packagePath = Path.Combine(packageDirectory, "manifest-package.json");

        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException($"Manifest Package descriptor not found at '{packagePath}'.", packagePath);
        }

        var package = JObject.Parse(File.ReadAllText(packagePath));
        this._environment = package.Value<string>("Environment") ?? string.Empty;

        var bySubjectKey = new Dictionary<string, JObject>(StringComparer.Ordinal);
        var byLegacyKey = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in package["Entries"] as JArray ?? new JArray())
        {
            var subject = entry.Value<string>("Subject");
            var parentSubject = entry.Value<string>("ParentSubject");
            var resourceTypeName = entry.Value<string>("ResourceTypeName");
            var resourceName = entry.Value<string>("ResourceName");
            var manifest = entry["Manifest"] as JObject;

            if (string.IsNullOrWhiteSpace(subject) || manifest is null)
            {
                continue;
            }

            bySubjectKey[MakeSubjectKey(subject, parentSubject)] = manifest;

            if (!string.IsNullOrWhiteSpace(resourceTypeName) && !string.IsNullOrWhiteSpace(resourceName))
            {
                byLegacyKey[$"{resourceTypeName}/{resourceName}"] = manifest;
            }
        }

        this._bySubjectKey = bySubjectKey;
        this._byLegacyKey = byLegacyKey;
    }

    /// <inheritdoc/>
    public string ProviderKind => "artifact";

    /// <inheritdoc/>
    public void StoreManifest(IGenerationScope scope, string resourceTypeName, string resourceName, object? manifestData) =>
        throw new NotSupportedException("ArtifactManifestProvider is read-only. Publish manifests via the producing project's own Manifest Package emission.");

    /// <inheritdoc/>
    public bool TryResolveProperty(IGenerationScope scope, string resourceTypeName, string resourceName, string propertyPath, out object? value)
    {
        if (!this.MatchesEnvironment(scope) || !this._byLegacyKey.TryGetValue($"{resourceTypeName}/{resourceName}", out var manifest))
        {
            value = null;
            return false;
        }

        return TryNavigate(manifest, propertyPath, out value);
    }

    /// <inheritdoc/>
    public bool HasManifest(IGenerationScope scope, string resourceTypeName, string resourceName) =>
        this.MatchesEnvironment(scope) && this._byLegacyKey.ContainsKey($"{resourceTypeName}/{resourceName}");

    /// <inheritdoc/>
    public void StoreManifestBySubject(IGenerationScope scope, string subject, string? parentManifest, object? manifestData) =>
        throw new NotSupportedException("ArtifactManifestProvider is read-only. Publish manifests via the producing project's own Manifest Package emission.");

    /// <inheritdoc/>
    public bool TryResolveBySubject(IGenerationScope scope, string subject, string? parentManifest, string propertyPath, out object? value)
    {
        if (!this.MatchesEnvironment(scope) || !this._bySubjectKey.TryGetValue(MakeSubjectKey(subject, parentManifest), out var manifest))
        {
            value = null;
            return false;
        }

        return TryNavigate(manifest, propertyPath, out value);
    }

    /// <inheritdoc/>
    public bool HasManifestBySubject(IGenerationScope scope, string subject, string? parentManifest) =>
        this.MatchesEnvironment(scope) && this._bySubjectKey.ContainsKey(MakeSubjectKey(subject, parentManifest));

    private bool MatchesEnvironment(IGenerationScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return string.Equals(scope.Environment, this._environment, StringComparison.OrdinalIgnoreCase);
    }

    private static string MakeSubjectKey(string subject, string? parentSubject) =>
        string.IsNullOrWhiteSpace(parentSubject) ? subject : $"{parentSubject}|{subject}";

    private static bool TryNavigate(JObject manifest, string propertyPath, out object? value)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            value = manifest;
            return true;
        }

        var token = manifest.SelectToken(propertyPath);

        if (token is null)
        {
            value = null;
            return false;
        }

        value = token.ToObject<object>();
        return true;
    }
}
