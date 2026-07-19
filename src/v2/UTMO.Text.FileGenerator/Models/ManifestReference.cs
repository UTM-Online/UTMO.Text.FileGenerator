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
    /// <summary>
    /// Initializes a subject-based manifest reference. This is the preferred way to declare a
    /// reference: the target manifest is identified purely by its <paramref name="subject"/>
    /// (optionally scoped by <paramref name="parentManifest"/>) and is resolved at runtime from
    /// the in-memory manifest index without holding the referenced resource instance.
    /// </summary>
    /// <param name="subject">The referenced manifest's subject identity (see <c>IManifestProducer.ManifestSubject</c>).</param>
    /// <param name="parentManifest">
    /// The optional parent manifest subject that scopes <paramref name="subject"/>. Pass
    /// <see langword="null"/> to resolve at the environment root scope.
    /// </param>
    /// <remarks>
    /// By default the entire referenced manifest object is injected into the template context.
    /// Set <see cref="PropertyPath"/> via an object initializer to inject a single nested value,
    /// e.g. <c>new ManifestReference("BaseConfig") { PropertyPath = "DependsOn" }</c>.
    /// </remarks>
    [SetsRequiredMembers]
    public ManifestReference(string subject, string? parentManifest = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        this.Subject        = subject;
        // Normalize a whitespace-only parent to null so it matches the "root scope" semantics
        // used by ManifestReferenceIndex/IManifestProvider, keeping DescribeTarget() and
        // validation diagnostics consistent with actual resolution behavior.
        this.ParentManifest = string.IsNullOrWhiteSpace(parentManifest) ? null : parentManifest;
        // Not used for subject-based resolution, but required members must still be assigned
        // to satisfy the [SetsRequiredMembers] contract on this constructor.
        this.ResourceTypeName = string.Empty;
        this.ResourceName     = string.Empty;
    }

    /// <summary>
    /// Initializes an empty manifest reference for the legacy object-initializer form that
    /// identifies the target by <see cref="ResourceTypeName"/>/<see cref="ResourceName"/>.
    /// Prefer the subject-based constructor for new code. <see cref="ResourceTypeName"/> and
    /// <see cref="ResourceName"/> are <see langword="required"/> so this form cannot be
    /// constructed without identifying the target resource.
    /// </summary>
    public ManifestReference()
    {
    }

    /// <summary>The subject identity of the referenced manifest (subject-based resolution).</summary>
    public string? Subject { get; init; }

    /// <summary>The optional parent manifest subject that scopes <see cref="Subject"/>.</summary>
    public string? ParentManifest { get; init; }

    /// <summary>The <see cref="ITemplateModel.ResourceTypeName"/> of the resource to look up (legacy resolution).</summary>
    public required string ResourceTypeName { get; init; }

    /// <summary>The <see cref="ITemplateModel.ResourceName"/> of the resource to look up (legacy resolution).</summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// A dot-separated path to the property within the manifest object returned by
    /// <see cref="Abstract.Contracts.IManifestProducer.ToManifest{TManifest}"/>.
    /// For example <c>"DependsOn"</c> or <c>"Network.SubnetId"</c>. An empty value (the default)
    /// resolves the entire manifest object.
    /// </summary>
    public string PropertyPath { get; init; } = string.Empty;

    /// <summary>
    /// The value to use when the reference cannot be resolved from the in-memory manifest
    /// index.  When <see langword="null"/> the reference is treated as <em>required</em>
    /// and an unresolved reference will cause generation to fail.  Provide a non-null
    /// string (including the empty string) to treat the reference as <em>optional</em>.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// The scope-resolution target for this reference (Manifest v2 phase P4, gap G9). Defaults
    /// to <see cref="ManifestReferenceTarget.ThisScope"/>, which preserves v1 behavior exactly.
    /// </summary>
    public ManifestReferenceTarget Target { get; init; } = ManifestReferenceTarget.ThisScope;

    /// <summary>
    /// A human-readable description of this reference's target, used in diagnostic logging.
    /// </summary>
    internal virtual string DescribeTarget() =>
        this.Subject is { } subject
            ? this.ParentManifest is { } parent
                ? $"subject '{parent}/{subject}#{this.PropertyPath}'"
                : $"subject '{subject}#{this.PropertyPath}'"
            : $"'{this.ResourceTypeName}/{this.ResourceName}#{this.PropertyPath}'";

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
        this.Subject is { } subject
            ? index.TryResolveBySubject(subject, this.ParentManifest, this.PropertyPath, out value)
            : index.TryResolveProperty(this.ResourceTypeName, this.ResourceName, this.PropertyPath, out value);

    /// <summary>
    /// Pre-render validation pass (Manifest v2 phase P2, gap G10): returns descriptive
    /// exceptions when this reference is dangling. The base (subject/legacy, untyped)
    /// implementation only checks presence; <see cref="ManifestReference{TSourceManifest}"/>
    /// additionally validates the resolved manifest's type (gap G3).
    /// </summary>
    internal virtual IEnumerable<Exception> ValidateNoThrow(IManifestProvider provider, IGenerationScope scope)
    {
        var found = this.Subject is { } subject
            ? provider.HasManifestBySubject(scope, subject, this.ParentManifest)
            : provider.HasManifest(scope, this.ResourceTypeName, this.ResourceName);

        if (!found)
        {
            yield return new InvalidOperationException(
                $"Dangling manifest reference: {this.DescribeTarget()} was not found in scope '{scope.GetIdentifier()}'.");
        }
    }
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

    [SetsRequiredMembers]
    private ManifestReference(
        Func<TSourceManifest, object?> propertyMapper,
        string subject,
        string? parentManifest,
        string? defaultValue)
        : base(subject, parentManifest)
    {
        ArgumentNullException.ThrowIfNull(propertyMapper);

        _propertyMapper   = propertyMapper;
        this.DefaultValue = defaultValue;
    }

    /// <summary>
    /// Creates a strongly typed, subject-based manifest reference. The referenced manifest is
    /// identified by <paramref name="subject"/> (optionally scoped by
    /// <paramref name="parentManifest"/>) and the injected value is selected via
    /// <paramref name="propertyMapper"/>.
    /// </summary>
    public static ManifestReference<TSourceManifest> BySubject(
        string subject,
        string? parentManifest,
        Func<TSourceManifest, object?> propertyMapper,
        string? defaultValue = null) =>
        new(propertyMapper, subject, parentManifest, defaultValue);

    /// <inheritdoc />
    internal override bool TryResolveValue(IManifestReferenceIndex index, out object? value)
    {
        var found = this.Subject is { } subject
            ? index.TryResolveBySubject(subject, this.ParentManifest, string.Empty, out var sourceManifest)
            : index.TryResolveProperty(this.ResourceTypeName, this.ResourceName, string.Empty, out sourceManifest);

        if (!found)
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

    /// <inheritdoc/>
    internal override IEnumerable<Exception> ValidateNoThrow(IManifestProvider provider, IGenerationScope scope)
    {
        var found = this.Subject is { } subject
            ? provider.TryGetManifest(scope, subject, this.ParentManifest, out var raw)
            : provider.TryResolveProperty(scope, this.ResourceTypeName, this.ResourceName, string.Empty, out raw);

        if (!found)
        {
            yield return new InvalidOperationException(
                $"Dangling manifest reference: {this.DescribeTarget()} was not found in scope '{scope.GetIdentifier()}'.");
            yield break;
        }

        if (raw is not TSourceManifest)
        {
            yield return new InvalidOperationException(
                $"Manifest reference type mismatch: {this.DescribeTarget()} resolved to " +
                $"'{raw?.GetType().Name ?? "null"}' but the reference expects '{typeof(TSourceManifest).Name}'.");
        }
    }
}
