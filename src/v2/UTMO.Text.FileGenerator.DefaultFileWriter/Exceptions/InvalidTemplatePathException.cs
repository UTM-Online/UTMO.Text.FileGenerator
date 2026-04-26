namespace UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

/// <summary>
/// Exception thrown when a template path is invalid or unsafe, such as when it contains
/// path traversal sequences, an absolute path, or otherwise escapes the allowed template directory.
/// </summary>
public class InvalidTemplatePathException(string templateName, string templateDirectory, string reason)
    : ApplicationException($"The template path \"{templateName}\" is invalid or unsafe: {reason}")
{
    /// <summary>Gets the configured template directory that the path must remain within.</summary>
    public string TemplateDirectory { get; } = templateDirectory;

    /// <summary>Gets the template name that failed validation.</summary>
    public string TemplateName { get; } = templateName;

    /// <summary>Gets a description of why the path was considered invalid.</summary>
    public string Reason { get; } = reason;
}

