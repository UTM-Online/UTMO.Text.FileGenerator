using UTMO.Text.FileGenerator.Abstract.Constants;

namespace UTMO.Text.FileGenerator.Plugins;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Constants;
using UTMO.Text.FileGenerator.Models;

/// <summary>
/// A <see cref="IPipelinePlugin"/> that runs at <see cref="PluginPosition.Before"/> and
/// populates the <see cref="IManifestReferenceIndex"/> from all
/// <see cref="IManifestProducer"/> resources in the current environment before template
/// rendering begins.
/// </summary>
/// <remarks>
/// <para>
/// This plugin is only active when the <c>ManifestReferenceResolution</c> feature flag is
/// enabled.  When the flag is disabled the plugin returns immediately without modifying the
/// index.
/// </para>
/// <para>
/// Cycle protection is provided by a per-call visited set: if the same
/// <c>ResourceTypeName/ResourceName</c> combination is encountered more than once during
/// the recursive traversal the second visit is skipped.
/// </para>
/// </remarks>
[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
public sealed class ManifestIndexBuildingPlugin : IPipelinePlugin
{
    private readonly IFeatureManager _featureManager;
    private readonly IManifestReferenceIndex _index;
    private readonly ILogger<ManifestIndexBuildingPlugin> _logger;

    public ManifestIndexBuildingPlugin(
        IFeatureManager featureManager,
        IManifestReferenceIndex index,
        ILogger<ManifestIndexBuildingPlugin> logger)
    {
        _featureManager = featureManager;
        _index          = index;
        _logger         = logger;
    }

    /// <inheritdoc/>
    public IGeneralFileWriter? Writer { get; init; }

    /// <inheritdoc/>
    public ITemplateGenerationEnvironment? Environment { get; init; }

    /// <inheritdoc/>
    public PluginPosition Position => PluginPosition.Before;

    /// <inheritdoc/>
    public TimeSpan MaxRuntime => TimeSpan.FromMinutes(5);

    /// <inheritdoc/>
    public bool RequiresGeneration => false;

    /// <inheritdoc/>
    public async Task<bool> ProcessPlugin(ITemplateGenerationEnvironment environment)
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution))
        {
            _logger.LogDebug(
                "Manifest reference resolution is disabled ({FeatureFlag}). Skipping index build for environment '{EnvironmentName}'.",
                FeatureFlags.EnableManifestReferenceResolution,
                environment.EnvironmentName);
            return true;
        }

        // Scope all index operations to this environment so manifests from different
        // environments cannot collide (each environment stores under its own key prefix).
        _index.BeginEnvironmentScope(environment.EnvironmentName);

        _logger.LogInformation(
            "Building manifest reference index for environment '{EnvironmentName}'.",
            environment.EnvironmentName);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var indexed = 0;

        try
        {
            foreach (var resource in environment.Resources)
            {
                indexed += await CollectManifestsFromResource(resource, visited);
            }

            _logger.LogInformation(
                "Manifest reference index built for environment '{EnvironmentName}': {IndexedCount} manifest(s) indexed.",
                environment.EnvironmentName,
                indexed);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error building manifest reference index for environment '{EnvironmentName}'.", environment.EnvironmentName);
            return false;
        }
    }

    /// <summary>
    /// Recursively walks <paramref name="resource"/> and any nested
    /// <see cref="ITemplateModel"/> properties, indexing each
    /// <see cref="IManifestProducer"/> whose <see cref="IManifestProducer.GenerateManifest"/>
    /// is <see langword="true"/>.
    /// </summary>
    /// <returns>The number of manifests indexed by this call (including nested).</returns>
    private async Task<int> CollectManifestsFromResource(ITemplateModel resource, HashSet<string> visited)
    {
        var resourceKey = $"{resource.ResourceTypeName}/{resource.ResourceName}";

        // Cycle protection: skip if we have already visited this resource in this traversal.
        if (!visited.Add(resourceKey))
        {
            _logger.LogDebug(
                "Skipping already-visited resource '{ResourceKey}' during manifest index traversal (cycle guard).",
                resourceKey);
            return 0;
        }

        var indexed = 0;

        // Walk public instance properties looking for nested ITemplateModel values.
        foreach (var prop in resource.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object? value;

            try
            {
                value = prop.GetValue(resource);
            }
            catch (TargetInvocationException ex)
            {
                // Log at debug level and continue; properties that throw during reflection
                // traversal (e.g. NotSupportedException, TargetInvocationException) are
                // skipped rather than halting the index build. Trace-visible configurations
                // will surface the full exception for diagnostics.
                _logger.LogDebug(
                    ex,
                    "Property '{PropertyName}' on '{ResourceType}' threw during traversal; skipping.",
                    prop.Name,
                    resource.GetType().Name);
                continue;
            }
            catch (TargetParameterCountException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Property '{PropertyName}' on '{ResourceType}' threw during traversal; skipping.",
                    prop.Name,
                    resource.GetType().Name);
                continue;
            }
            catch (MethodAccessException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Property '{PropertyName}' on '{ResourceType}' threw during traversal; skipping.",
                    prop.Name,
                    resource.GetType().Name);
                continue;
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Property '{PropertyName}' on '{ResourceType}' threw during traversal; skipping.",
                    prop.Name,
                    resource.GetType().Name);
                continue;
            }
            catch (TargetException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Property '{PropertyName}' on '{ResourceType}' threw during traversal; skipping.",
                    prop.Name,
                    resource.GetType().Name);
                continue;
            }
            catch (NotSupportedException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Property '{PropertyName}' on '{ResourceType}' threw during traversal; skipping.",
                    prop.Name,
                    resource.GetType().Name);
                continue;
            }

            switch (value)
            {
                // Rely on the visited set for cycle detection; no need to compare names here.
                case ITemplateModel nested:
                    indexed += await CollectManifestsFromResource(nested, visited);
                    break;

                case IEnumerable<ITemplateModel> nestedList:
                {
                    foreach (var item in nestedList)
                    {
                        indexed += await CollectManifestsFromResource(item, visited);
                    }

                    break;
                }
            }
        }

        // Index this resource's manifest if it is a manifest producer.
        if (resource is IManifestProducer { GenerateManifest: true } producer)
        {
            var manifestData = await producer.ToManifest<IManifest>();
            _index.StoreManifest(resource.ResourceTypeName, resource.ResourceName, manifestData);
            indexed++;

            _logger.LogDebug(
                "Indexed manifest for resource '{ResourceTypeName}/{ResourceName}'.",
                resource.ResourceTypeName,
                resource.ResourceName);
        }
        else
        {
            _logger.LogTrace(
                "Resource '{ResourceTypeName}/{ResourceName}' is not a manifest producer or has GenerateManifest=false; skipping index.",
                resource.ResourceTypeName,
                resource.ResourceName);
        }

        return indexed;
    }
}
