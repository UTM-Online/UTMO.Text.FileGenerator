using UTMO.Text.FileGenerator.Abstract.Constants;

namespace UTMO.Text.FileGenerator.Plugins;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Models;

/// <summary>
/// A <see cref="IPipelinePlugin"/> that runs at <see cref="PluginPosition.Before"/>, after
/// <see cref="ManifestIndexBuildingPlugin"/>, and validates every declared
/// <see cref="ManifestReference"/> before any template is rendered (Manifest v2 phase P2, gaps
/// G3/G10). Dangling subjects and type mismatches are reported here, up front, rather than
/// silently falling back to <see cref="ManifestReference.DefaultValue"/> at render time or
/// surfacing mid-render.
/// </summary>
/// <remarks>
/// Gated by the <c>EnableManifestReferenceValidation</c> feature flag, which is additive to
/// (and requires) <c>EnableManifestReferenceResolution</c>. When disabled the plugin returns
/// immediately without validating anything, preserving v1/current behavior.
/// </remarks>
[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
public sealed class ManifestReferenceValidationPlugin : IPipelinePlugin
{
    private readonly IFeatureManager _featureManager;
    private readonly IManifestProvider _provider;
    private readonly ILogger<ManifestReferenceValidationPlugin> _logger;

    public ManifestReferenceValidationPlugin(
        IFeatureManager featureManager,
        IManifestProvider provider,
        ILogger<ManifestReferenceValidationPlugin> logger)
    {
        _featureManager = featureManager;
        _provider       = provider;
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
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution) ||
            !await _featureManager.IsEnabledAsync(FeatureFlags.EnableManifestReferenceValidation))
        {
            _logger.LogDebug(
                "Manifest reference validation is disabled ({FeatureFlag}). Skipping validation for environment '{EnvironmentName}'.",
                FeatureFlags.EnableManifestReferenceValidation,
                environment.EnvironmentName);
            return true;
        }

        var scope = GenerationScope.ForEnvironment(environment.EnvironmentName);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var failureCount = 0;

        foreach (var resource in environment.Resources)
        {
            failureCount += CollectAndValidate(resource, scope, visited);
        }

        if (failureCount > 0)
        {
            _logger.LogError(
                "Manifest reference validation failed for environment '{EnvironmentName}': {FailureCount} invalid reference(s). See preceding errors for details.",
                environment.EnvironmentName,
                failureCount);
            return false;
        }

        _logger.LogInformation(
            "Manifest reference validation passed for environment '{EnvironmentName}'.",
            environment.EnvironmentName);
        return true;
    }

    private int CollectAndValidate(ITemplateModel resource, IGenerationScope scope, HashSet<string> visited)
    {
        var resourceKey = $"{resource.ResourceTypeName}/{resource.ResourceName}";

        if (!visited.Add(resourceKey))
        {
            return 0;
        }

        var failureCount = 0;

        foreach (var prop in resource.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object? value;

            try
            {
                value = prop.GetValue(resource);
            }
            catch (Exception ex) when (ex is TargetInvocationException or TargetParameterCountException
                                            or MethodAccessException or ArgumentException
                                            or TargetException or NotSupportedException)
            {
                continue;
            }

            switch (value)
            {
                case ITemplateModel nested:
                    failureCount += CollectAndValidate(nested, scope, visited);
                    break;

                case IEnumerable<ITemplateModel> nestedList:
                {
                    foreach (var item in nestedList)
                    {
                        failureCount += CollectAndValidate(item, scope, visited);
                    }

                    break;
                }
            }
        }

        if (resource is not TemplateResourceBase templateResource)
        {
            return failureCount;
        }

        foreach (var (contextKey, reference) in templateResource.ManifestReferences)
        {
            foreach (var error in reference.ValidateNoThrow(_provider, scope))
            {
                _logger.LogError(
                    "Manifest reference validation failed for context key '{ContextKey}' on resource '{ResourceTypeName}/{ResourceName}': {ErrorMessage}",
                    contextKey,
                    resource.ResourceTypeName,
                    resource.ResourceName,
                    error.Message);
                failureCount++;
            }
        }

        return failureCount;
    }
}
