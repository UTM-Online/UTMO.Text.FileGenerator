namespace UTMO.Text.FileGenerator.Exceptions;

/// <summary>
/// Thrown when a required manifest reference cannot be resolved from the in-memory manifest
/// index during the pre-render phase of file generation.
/// </summary>
public sealed class ManifestReferenceResolutionException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of <see cref="ManifestReferenceResolutionException"/>.
    /// </summary>
    /// <param name="sourceResourceName">The resource that declared the reference.</param>
    /// <param name="sourceResourceTypeName">The resource type of the declaring resource.</param>
    /// <param name="referencedResourceTypeName">The resource type that was looked up.</param>
    /// <param name="referencedResourceName">The resource name that was looked up.</param>
    /// <param name="propertyPath">The property path that could not be resolved.</param>
    /// <param name="message">A human-readable description of the failure.</param>
    public ManifestReferenceResolutionException(
        string sourceResourceName,
        string sourceResourceTypeName,
        string referencedResourceTypeName,
        string referencedResourceName,
        string propertyPath,
        string message)
        : base(message)
    {
        SourceResourceName         = sourceResourceName;
        SourceResourceTypeName     = sourceResourceTypeName;
        ReferencedResourceTypeName = referencedResourceTypeName;
        ReferencedResourceName     = referencedResourceName;
        PropertyPath               = propertyPath;
    }

    /// <summary>The resource name of the template that declared this reference.</summary>
    public string SourceResourceName { get; }

    /// <summary>The resource type name of the template that declared this reference.</summary>
    public string SourceResourceTypeName { get; }

    /// <summary>The resource type name that was referenced but could not be resolved.</summary>
    public string ReferencedResourceTypeName { get; }

    /// <summary>The resource name that was referenced but could not be resolved.</summary>
    public string ReferencedResourceName { get; }

    /// <summary>The dot-separated property path that could not be resolved.</summary>
    public string PropertyPath { get; }
}
