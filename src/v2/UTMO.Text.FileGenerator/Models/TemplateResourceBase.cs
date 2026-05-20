namespace UTMO.Text.FileGenerator.Models;

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.Attributes;
using UTMO.Text.FileGenerator.Constants;

public abstract class TemplateResourceBase : ITemplateModel, IManifestProducer
{
    // ReSharper disable once CollectionNeverUpdated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    protected readonly Dictionary<string, object> TemplateConstants = new();

    private readonly Dictionary<string, ManifestReference> _manifestReferences = new();

    [IgnoreMember]
    protected internal IFeatureManager? FeatureManager { get; internal set; }
    
    [IgnoreMember]
    protected internal ILogger? Logger { get; internal set; }

    public virtual bool GenerateManifest { get; } = false;

    public virtual Task<object?> ToManifest()
    {
        return Task.FromResult(null as object);
    }

    public abstract string ResourceTypeName { get; }

    public abstract string TemplatePath { get; }

    public abstract string OutputExtension { get; }

    public abstract string ResourceName { get; }

    public virtual bool EnableGeneration { get; } = true;

    public virtual bool UseAlternateName { get; } = false;

    public virtual Task<List<ValidationFailedException>> Validate()
    {
        return Task.FromResult(new List<ValidationFailedException>());
    }

    public virtual async Task<Dictionary<string, object>> ToTemplateContext()
    {
        var properties = new Dictionary<string, object>();
        var allowLegacyNonPublicTemplateProperties = await this.IsLegacyNonPublicTemplatePropertyExposureEnabled();
        var suppressNonPublicPropertyWarnings = !allowLegacyNonPublicTemplateProperties
            && await this.IsNonPublicPropertyWarningSuppressed();

        foreach (var prop in this.GetProperties(allowLegacyNonPublicTemplateProperties, suppressNonPublicPropertyWarnings))
        {
            var propertyName  = prop.GetCustomAttribute<MemberNameAttribute>(true)?.Name ?? prop.Name;
            var propertyValue = prop.GetValue(this);

            switch (propertyValue)
            {
                case null:
                    continue;

                case TemplateResourceBase templateResource:
                    templateResource.FeatureManager ??= this.FeatureManager;
                    templateResource.Logger ??= this.Logger;
                    properties.Add(propertyName, await templateResource.ToTemplateContext());
                    break;

                case IEnumerable<TemplateResourceBase> resources:
                {
                    if (this.FeatureManager is not null && await this.FeatureManager.IsEnabledAsync(FeatureFlags.EnableParallelPropertyRendering))
                    {
                        var resourceList = new ConcurrentBag<Dictionary<string, object>>();
                        await Parallel.ForEachAsync(resources, async (resource, token) =>
                                                                {
                                                                    resource.FeatureManager ??= this.FeatureManager;
                                                                    resource.Logger ??= this.Logger;
                                                                    resourceList.Add(await resource.ToTemplateContext().WaitAsync(token));
                                                                });
                        properties.Add(propertyName, resourceList);
                    }
                    else
                    {
                        var resourceList = new List<Dictionary<string, object>>();

                        foreach (var resource in resources)
                        {
                            resource.FeatureManager ??= this.FeatureManager;
                            resource.Logger ??= this.Logger;
                            resourceList.Add(await resource.ToTemplateContext());
                        }

                        properties.Add(propertyName, resourceList);
                    }

                    break;
                }

                default:
                    properties.Add(propertyName, propertyValue);
                    break;
            }
        }

        // ReSharper disable once InvertIf
        if (this.TemplateConstants.Count != 0)
        {
            foreach (var prop in this.TemplateConstants)
            {
                properties.Add(prop.Key, prop.Value);
            }
        }

        return properties;
    }

    public virtual string ProduceOutputPath(string basePath)
    {
        var path = this.UseAlternateName
                       ? $"{this.ResourceName}.{this.ResourceTypeName}.{this.OutputExtension.TrimStart('.')}"
                       : $"{this.ResourceName}.{this.OutputExtension.TrimStart('.')}";
        return Path.Join(basePath, this.ResourceTypeName, path);
    }

    public virtual ITemplateModel AddAdditionalProperty<T>(string key, T value)
    {
        this.TemplateConstants.Add(key, value!);
        return this;
    }

    /// <summary>
    /// Declares a manifest reference that will be resolved before this resource is rendered
    /// (when the <c>ManifestReferenceResolution</c> feature flag is enabled).
    /// The resolved value is injected into the template context under <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The template-context key under which the resolved value will appear.</param>
    /// <param name="reference">The reference descriptor.</param>
    protected void AddManifestReference(string key, ManifestReference reference) =>
        _manifestReferences[key] = reference;

    /// <summary>
    /// The manifest references declared by this resource.  Consumed internally by
    /// <c>ManifestReferenceResolverPlugin</c> before each render.
    /// </summary>
    internal IReadOnlyDictionary<string, ManifestReference> ManifestReferences => _manifestReferences;

    private static readonly ConcurrentDictionary<string, byte> MissingTemplatePropertyLogs = new();
    private static readonly ConcurrentDictionary<string, byte> MissingNonPublicTemplatePropertyLogs = new();
    private static readonly ConcurrentDictionary<string, byte> LegacyPublicTemplatePropertyLogs = new();
    private static readonly ConcurrentDictionary<string, byte> LegacyNonPublicExposedPropertyLogs = new();

    private IEnumerable<PropertyInfo> GetProperties(bool allowLegacyNonPublicTemplateProperties, bool suppressNonPublicPropertyWarnings)
    {
        var allProperties = this.GetType()
                                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var prop in allProperties)
        {
            // Skip properties with IgnoreMemberAttribute
            if (prop.GetCustomAttributes<IgnoreMemberAttribute>(true).Any())
            {
                continue;
            }

            // Check if property has TemplatePropertyAttribute
            var hasTemplateProperty = prop.GetCustomAttribute<TemplatePropertyAttribute>(true) != null;
            var isPublic = prop.GetMethod?.IsPublic == true;

            // New secure behavior: Only expose properties with [TemplateProperty] attribute
            if (hasTemplateProperty && isPublic)
            {
                yield return prop;
                continue;
            }

            // Legacy/Deprecated behavior: Log warnings for non-public properties or public properties without attribute
            if (!isPublic && hasTemplateProperty)
            {
                // Non-public property with TemplateProperty attribute - this is not allowed
                this.Logger?.LogWarning(
                    "Property '{PropertyName}' on type '{TypeName}' is marked with [TemplateProperty] but is not public. " +
                    "Only public properties can be exposed to templates. This property will be ignored.",
                    prop.Name,
                    this.GetType().Name);
                continue;
            }

            if (!hasTemplateProperty && !isPublic)
            {
                if (!allowLegacyNonPublicTemplateProperties)
                {
                    if (!suppressNonPublicPropertyWarnings && this.ShouldLogMissingNonPublicTemplateProperty(prop))
                    {
                        this.Logger?.LogWarning(
                            "Non-public property '{PropertyName}' on type '{TypeName}' is not marked with [TemplateProperty] and will not be exposed to templates. " +
                            "Legacy non-public template exposure is disabled by default. " +
                            "For migration only, enable feature flag '{FeatureFlagName}' to temporarily restore the previous behavior.",
                            prop.Name,
                            this.GetType().Name,
                            FeatureFlags.EnableLegacyNonPublicTemplateProperties);
                    }

                    continue;
                }

                // Legacy migration behavior gated behind explicit feature flag.
                // Track for end-of-execution summary instead of logging immediately.
                this.TrackLegacyNonPublicExposedTemplateProperty(prop);
                yield return prop;
                continue;
            }

            if (!hasTemplateProperty && isPublic)
            {
                if (allowLegacyNonPublicTemplateProperties)
                {
                    // Legacy migration behavior: also expose public properties without [TemplateProperty]
                    // when the feature flag is enabled, restoring the full pre-v2.16 property exposure
                    // (which exposed ALL properties, not just non-public ones).
                    // Track once per type+property for end-of-execution summary instead of logging immediately.
                    this.TrackLegacyPublicTemplateProperty(prop);
                    yield return prop;
                    continue;
                }

                // Public property without TemplateProperty attribute
                // This is now opt-in, so we don't expose it anymore.
                // Log once per type/property at Debug to avoid high-volume migration noise.
                if (this.ShouldLogMissingTemplateProperty(prop))
                {
                    this.Logger?.LogDebug(
                        "Public property '{PropertyName}' on type '{TypeName}' is not marked with [TemplateProperty] and will not be exposed to templates. " +
                        "Add [TemplateProperty] attribute if this property should be accessible in templates.",
                        prop.Name,
                        this.GetType().Name);
                }
            }
        }
    }

    private void TrackLegacyPublicTemplateProperty(PropertyInfo prop)
    {
        var typeName = this.GetType().FullName ?? this.GetType().Name;
        LegacyPublicTemplatePropertyLogs.TryAdd($"{typeName}:{prop.Name}", 0);
    }

    private void TrackLegacyNonPublicExposedTemplateProperty(PropertyInfo prop)
    {
        var typeName = this.GetType().FullName ?? this.GetType().Name;
        LegacyNonPublicExposedPropertyLogs.TryAdd($"{typeName}:{prop.Name}", 0);
    }

    private bool ShouldLogMissingTemplateProperty(PropertyInfo prop)
    {
        var typeName = this.GetType().FullName ?? this.GetType().Name;
        return MissingTemplatePropertyLogs.TryAdd($"{typeName}:{prop.Name}", 0);
    }

    private bool ShouldLogMissingNonPublicTemplateProperty(PropertyInfo prop)
    {
        var typeName = this.GetType().FullName ?? this.GetType().Name;
        return MissingNonPublicTemplatePropertyLogs.TryAdd($"{typeName}:{prop.Name}", 0);
    }

    private async Task<bool> IsLegacyNonPublicTemplatePropertyExposureEnabled()
    {
        if (this.FeatureManager is null)
        {
            return false;
        }

        return await this.FeatureManager.IsEnabledAsync(FeatureFlags.EnableLegacyNonPublicTemplateProperties);
    }

    private async Task<bool> IsNonPublicPropertyWarningSuppressed()
    {
        if (this.FeatureManager is null)
        {
            return false;
        }

        return await this.FeatureManager.IsEnabledAsync(FeatureFlags.SuppressNonPublicPropertyWarnings);
    }

    internal static (IReadOnlyDictionary<string, IReadOnlyList<string>> PublicByType, IReadOnlyDictionary<string, IReadOnlyList<string>> NonPublicByType) GetLegacyExposedPropertiesSummary()
    {
        static IReadOnlyDictionary<string, IReadOnlyList<string>> GroupByType(ConcurrentDictionary<string, byte> dict)
        {
            return dict.Keys
                       .Select(k => k.Split(':', 2))
                       .Where(p => p.Length == 2)
                       .GroupBy(p => p[0], p => p[1])
                       .OrderBy(g => g.Key, StringComparer.Ordinal)
                       .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.OrderBy(n => n, StringComparer.Ordinal).ToArray());
        }

        return (GroupByType(LegacyPublicTemplatePropertyLogs), GroupByType(LegacyNonPublicExposedPropertyLogs));
    }

    private static void ResetMissingTemplatePropertyLogsForTesting()
    {
        MissingTemplatePropertyLogs.Clear();
        MissingNonPublicTemplatePropertyLogs.Clear();
        LegacyPublicTemplatePropertyLogs.Clear();
        LegacyNonPublicExposedPropertyLogs.Clear();
    }
}