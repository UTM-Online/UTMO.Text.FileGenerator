namespace UTMO.Text.FileGenerator.Abstract.Exceptions;

/// <summary>
/// Exception thrown when template rendering fails.
/// SECURITY: This exception does not store the full template context to prevent
/// sensitive data (credentials, API keys, PII) from being exposed in logs via
/// exception details enrichment (e.g., Serilog's WithExceptionDetails()).
/// Only safe metadata is stored: template name, output path, and context structure.
/// </summary>
public class TemplateRenderingException : ApplicationException
{
    public TemplateRenderingException(string message, Dictionary<string,object>? model, string outputPath, string templateName) 
        : base($"An exception occurred rendering template {templateName} to {outputPath} with message {message}")
    {
        this.TemplateName = templateName;
        this.OutputFileName = outputPath;
        this.ContextKeyCount = model?.Count ?? 0;
        this.ContextKeys = model?.Keys.OrderBy(k => k).ToList() ?? new List<string>();
    }
    
    public TemplateRenderingException(string message, Dictionary<string,object>? model, string outputPath, string templateName, Exception innerException) 
        : base($"An exception occurred rendering template {templateName} to {outputPath} with message {message}", innerException)
    {
        this.TemplateName = templateName;
        this.OutputFileName = outputPath;
        this.ContextKeyCount = model?.Count ?? 0;
        this.ContextKeys = model?.Keys.OrderBy(k => k).ToList() ?? new List<string>();
    }
    
    /// <summary>
    /// Gets the template name.
    /// </summary>
    public string TemplateName { get; set; }
    
    /// <summary>
    /// Gets the output file path.
    /// </summary>
    public string OutputFileName { get; set; }
    
    /// <summary>
    /// Gets the number of keys in the template context.
    /// Safe to log - only provides count, not actual data.
    /// </summary>
    public int ContextKeyCount { get; set; }
    
    /// <summary>
    /// Gets the keys from the template context (names only, no values).
    /// Safe to log - reveals structure without sensitive values.
    /// </summary>
    public List<string> ContextKeys { get; set; }
    
    /// <SECURITY_NOTE>
    /// The full template context (Model) is NOT stored in this exception.
    /// This prevents sensitive data from being exposed when exceptions are logged
    /// with Serilog's WithExceptionDetails() enricher.
    /// </SECURITY_NOTE>
}