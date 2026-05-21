namespace UTMO.Text.FileGenerator.Models;

using System.Diagnostics.CodeAnalysis;
using UTMO.Text.FileGenerator.Abstract.Contracts;

/// <summary>
/// Represents a reference to a specific property in another resource's manifest output.
/// Declare manifest references on a <see cref="TemplateResourceBase"/> by calling
/// <see cref="TemplateResourceBase.AddManifestReference"/> in the resource's constructor or
/// initializer. The framework resolves each reference before the template is rendered and
/// injects the resolved value into the template context under the key used when the
/// reference was registered.
/// </summary>
/// <remarks>
/// <para>
/// Manifest references require the <c>ManifestReferenceResolution</c> feature flag to be
/// enabled. When the flag is disabled the references are silently ignored and the template
/// context key is not populated.
/// </para>
/// <para>
/// <b>Example (PowerShell DSC cross-configuration dependency):</b>
/// <code>
/// AddManifestReference("DependsOn", new ManifestReference
/// {
///     ResourceTypeName = "NodeConfiguration",
///     ResourceName     = "BaseConfig",
///     PropertyPath     = "DependsOn",
///     DefaultValue     = null   // null means "required – fail if not found"
/// });
/// </code>
/// </para>
/// </remarks>
public class ManifestReference
{
    /// <summary>The <see cref="ITemplateModel.ResourceTypeName"/> of the resource to look up.</summary>
    public required string ResourceTypeName { get; init; }

    /// <summary>The <see cref="ITemplateModel.ResourceName"/> of the resource to look up.</summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// A dot-separated path to the property within the manifest object returned by
    /// <see cref="Abstract.Contracts.IManifestProducer.ToManifest{TManifest}"/>.
    /// For example <c>"DependsOn"</c> or <c>"Network.SubnetId"</c>.
    /// </summary>
    public required string PropertyPath { get; init; }

    /// <summary>
    /// The value to use when the reference cannot be resolved from the in-memory manifest
    /// index.  When <see langword="null"/> the reference is treated as <em>required</em>
    /// and an unresolved reference will cause generation to fail.  Provide a non-null
    /// string (including the empty string) to treat the reference as <em>optional</em>.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Resolves this reference against the in-memory index.
    /// </summary>
    /// <param name="index">The in-memory manifest index.</param>
    /// <param name="value">The resolved value when the method returns <see langword="true"/>.</param>
    /// <returns>
    /// <see langword="true"/> when resolution succeeded; otherwise <see langword="false"/>.
    /// Derived implementations should return <see langword="false"/> when the source manifest
    /// does not match the expected shape/type so optional-reference fallback behavior can apply.
    /// </returns>
    internal virtual bool TryResolveValue(IManifestReferenceIndex index, out object? value) =>
        index.TryResolveProperty(this.ResourceTypeName, this.ResourceName, this.PropertyPath, out value);
}

/// <summary>
/// Represents a strongly typed manifest reference where the target value is selected from
/// a typed manifest model via <paramref name="propertyMapper"/>.
/// </summary>
/// <typeparam name="TSourceManifest">The manifest model type returned by the referenced resource.</typeparam>
public sealed class ManifestReference<TSourceManifest> : ManifestReference where TSourceManifest : class, IManifest
{
    private readonly Func<TSourceManifest, object?> _propertyMapper;

    /// <summary>
    /// Initializes a new generic manifest reference.
    /// </summary>
    /// <param name="resourceTypeName">
    /// The <see cref="ITemplateModel.ResourceTypeName"/> of the resource whose manifest is
    /// being referenced.  This must match the value returned by the referenced resource's
    /// <see cref="ITemplateModel.ResourceTypeName"/> property, which is the key used by
    /// <c>ManifestIndexBuildingPlugin</c> when storing the manifest in the index.
    /// </param>
    /// <param name="resourceName">The referenced resource name.</param>
    /// <param name="propertyMapper">
    /// A mapping function that selects the value to inject from the source manifest model.
    /// </param>
    /// <param name="defaultValue">
    /// Optional fallback value used when the reference cannot be resolved.
    /// </param>
    [SetsRequiredMembers]
    public ManifestReference(
        string resourceTypeName,
        string resourceName,
        Func<TSourceManifest, object?> propertyMapper,
        string? defaultValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(propertyMapper);

        _propertyMapper       = propertyMapper;
        this.ResourceTypeName = resourceTypeName;
        this.ResourceName     = resourceName;
        this.PropertyPath     = string.Empty;
        this.DefaultValue     = defaultValue;
    }

    /// <inheritdoc />
    internal override bool TryResolveValue(IManifestReferenceIndex index, out object? value)
    {
        if (!index.TryResolveProperty(this.ResourceTypeName, this.ResourceName, string.Empty, out var sourceManifest))
        {
            value = null;
            return false;
        }

        if (sourceManifest is not TSourceManifest typedSourceManifest)
        {
            value = null;
            return false;
        }

        value = _propertyMapper(typedSourceManifest);
        return true;
    }
}
