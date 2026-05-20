namespace UTMO.Text.FileGenerator.Models;

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
public sealed class ManifestReference
{
    /// <summary>The <see cref="ITemplateModel.ResourceTypeName"/> of the resource to look up.</summary>
    public required string ResourceTypeName { get; init; }

    /// <summary>The <see cref="ITemplateModel.ResourceName"/> of the resource to look up.</summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// A dot-separated path to the property within the manifest object returned by
    /// <see cref="Abstract.Contracts.IManifestProducer.ToManifest"/>.
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
}
