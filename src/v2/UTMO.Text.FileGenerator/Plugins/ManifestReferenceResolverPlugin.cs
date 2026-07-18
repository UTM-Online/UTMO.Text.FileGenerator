using UTMO.Text.FileGenerator.Abstract.Constants;

namespace UTMO.Text.FileGenerator.Plugins;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Constants;
using UTMO.Text.FileGenerator.Models;

/// <summary>
/// An <see cref="IRenderingPipelinePlugin"/> that runs at <see cref="PluginPosition.Before"/>
/// and resolves all <see cref="ManifestReference"/> declarations on a
/// <see cref="TemplateResourceBase"/> before the template is rendered.
/// </summary>
/// <remarks>
/// <para>
/// Resolved values are injected into the template context via
/// <see cref="Abstract.Contracts.ITemplateModel.AddAdditionalProperty{T}"/> using the same
/// key that was supplied when <see cref="TemplateResourceBase.AddManifestReference"/> was
/// called.
/// </para>
/// <para>
/// When a required reference (one with <see cref="ManifestReference.DefaultValue"/> equal
/// to <see langword="null"/>) cannot be resolved the plugin logs an error and returns
/// <see langword="false"/>, causing <c>IsSuccessfulRun</c> to be set to
/// <see langword="false"/> in <c>FileGeneratorHost</c>.
/// </para>
/// <para>
/// When an optional reference (non-null <see cref="ManifestReference.DefaultValue"/>) cannot
/// be resolved the default value is injected instead and a warning is logged.
/// </para>
/// </remarks>
[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
public sealed class ManifestReferenceResolverPlugin : IRenderingPipelinePlugin
{
    private readonly IFeatureManager _featureManager;
    private readonly IManifestReferenceIndex _index;
    private readonly ILogger<ManifestReferenceResolverPlugin> _logger;

    public ManifestReferenceResolverPlugin(
        IFeatureManager featureManager,
        IManifestReferenceIndex index,
        ILogger<ManifestReferenceResolverPlugin> logger)
    {
        _featureManager = featureManager;
        _index          = index;
        _logger         = logger;
        Writer          = null!;
        Environment     = null!;
    }

    /// <inheritdoc/>
    public IGeneralFileWriter Writer { get; init; }

    /// <inheritdoc/>
    public ITemplateGenerationEnvironment Environment { get; init; }

    /// <inheritdoc/>
    public PluginPosition Position => PluginPosition.Before;

    /// <inheritdoc/>
    public TimeSpan MaxRuntime => TimeSpan.FromMinutes(5);

    /// <inheritdoc/>
    public bool RequiresGeneration => false;

    /// <inheritdoc/>
    public async Task<bool> HandleTemplate(ITemplateModel model)
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.EnableManifestReferenceResolution))
        {
            return true;
        }

        if (model is not TemplateResourceBase resource)
        {
            return true;
        }

        var references = resource.ManifestReferences;

        if (references.Count == 0)
        {
            return true;
        }

        _logger.LogDebug(
            "Resolving {ReferenceCount} manifest reference(s) for resource '{ResourceTypeName}/{ResourceName}'.",
            references.Count,
            resource.ResourceTypeName,
            resource.ResourceName);

        var allResolved = true;

        foreach (var (contextKey, reference) in references)
        {
            _logger.LogDebug(
                "Resolving manifest reference for context key '{ContextKey}': {ReferenceTarget} " +
                "(source: '{SourceTypeName}/{SourceName}').",
                contextKey,
                reference.DescribeTarget(),
                resource.ResourceTypeName,
                resource.ResourceName);

            if (reference.TryResolveValue(_index, out var resolvedValue))
            {
                resource.AddAdditionalProperty(contextKey, resolvedValue);

                _logger.LogDebug(
                    "Manifest reference resolved: context key '{ContextKey}' for resource " +
                    "'{SourceTypeName}/{SourceName}' → reference {ReferenceTarget}.",
                    contextKey,
                    resource.ResourceTypeName,
                    resource.ResourceName,
                    reference.DescribeTarget());
            }
            else if (reference.DefaultValue is not null)
            {
                // Optional reference – fall back to the declared default.
                resource.AddAdditionalProperty(contextKey, reference.DefaultValue);

                _logger.LogWarning(
                    "Manifest reference unresolved for context key '{ContextKey}' on resource " +
                    "'{SourceTypeName}/{SourceName}': reference {ReferenceTarget} " +
                    "was not found in the index. Using declared default value.",
                    contextKey,
                    resource.ResourceTypeName,
                    resource.ResourceName,
                    reference.DescribeTarget());
            }
            else
            {
                // Required reference – generation cannot proceed.
                _logger.LogError(
                    "Required manifest reference unresolved: context key '{ContextKey}' on resource " +
                    "'{SourceTypeName}/{SourceName}' references {ReferenceTarget} " +
                    "which was not found in the manifest index. " +
                    "Ensure the referenced resource has GenerateManifest=true and that the ManifestReferenceResolution " +
                    "feature flag is enabled.",
                    contextKey,
                    resource.ResourceTypeName,
                    resource.ResourceName,
                    reference.DescribeTarget());

                allResolved = false;
            }
        }

        return allResolved;
    }
}
