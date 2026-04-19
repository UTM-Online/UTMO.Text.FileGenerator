namespace UTMO.Text.FileGenerator;

using Abstract.Exceptions;
using Constants;
using DotLiquid;
using Extensions;
using Microsoft.Extensions.Logging;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

/// <summary>
/// Renders Liquid templates to generate text files with support for global context injection.
/// </summary>
public class TemplateRenderer : ITemplateRenderer
{
    public TemplateRenderer(IGeneratorCliOptions options, IGeneralFileWriter fileWriter, ILogger<TemplateRenderer> logger)
    {
        this.FileWriter = fileWriter;
        this.GlobalContext = new Dictionary<string, object>();
        this.TemplatePath = options.TemplatePath;
        this.Logger = logger;
    }
    
    /// <summary>
    /// Generates a file from a Liquid template using a dictionary context.
    /// </summary>
    /// <param name="templateName">The name of the template file (will auto-append .liquid if not present).</param>
    /// <param name="outputFileName">The full path where the generated file should be written.</param>
    /// <param name="dict">The data context for template rendering.</param>
    /// <exception cref="TemplateNotFoundException">Thrown when the template file cannot be found.</exception>
    /// <exception cref="TemplateRenderingException">Thrown when template rendering fails.</exception>
    /// <exception cref="NoGeneratedTextException">Thrown when the template produces no output.</exception>
    public async Task GenerateFile(string templateName, string outputFileName, Dictionary<string, object> dict)
    {
        // Validate template path against path traversal attacks first, before any processing
        ValidateTemplatePath(templateName);
        
        if (!templateName.EndsWith(GenerationConstants.LiquidTemplateExtension))
        {
            templateName = string.Concat(templateName, GenerationConstants.LiquidTemplateExtension);
        }
        
        if (this.GlobalContext.Count > 0)
        {
            dict = dict.Merge(this.GlobalContext);
        }
        
        var templatePath = Path.Combine(this.TemplatePath, templateName);
        
        if (!File.Exists(templatePath))
        {
            var ex = new TemplateNotFoundException(templateName, this.TemplatePath);
            this.Logger.LogError(ex, "Template {TemplateName} not found in {TemplateSearchPath}", templateName, this.TemplatePath);
            throw ex;
        }
        
        var    templateText   = await File.ReadAllTextAsync(templatePath);
        var    parsedTemplate = Template.Parse(templateText);
        string results;
        
        try
        {
            results = parsedTemplate.Render(Hash.FromDictionary(dict));
        }
        catch (Exception ex)
        {
            if (ex is DirectoryNotFoundException or FileNotFoundException)
            {
                var tnfEx = new TemplateNotFoundException(templateName, this.TemplatePath);
                this.Logger.LogError(tnfEx, "Template {TemplateName} not found in {TemplateSearchPath}", templateName, this.TemplatePath);
                throw tnfEx;
            }
            
            this.Logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
            throw new TemplateRenderingException($"Failed to render template {templateName}", dict, outputFileName, templateName, ex);
        }
        
        if (string.IsNullOrWhiteSpace(results))
        {
            var noGeneratedTextException = new NoGeneratedTextException(templateName, outputFileName);
            this.Logger.LogError(noGeneratedTextException, "No text generated for template {TemplateName} to {OutPutFileName}", templateName, outputFileName);
            throw noGeneratedTextException;
        }
        
        // Check for DotLiquid error messages in the output
        if (results.StartsWith("Liquid error: Error - Illegal template path"))
        {
            var invalidTemplatePathException = new InvalidTemplateDirectoryException(templateName, this.TemplatePath);
            this.Logger.LogError(invalidTemplatePathException, "Invalid template path for template {TemplateName} in {TemplateSearchPath}", templateName, this.TemplatePath);
            throw invalidTemplatePathException;
        }
        
        ValidateTemplateOutput(results, dict, outputFileName, templateName);
        
        await this.FileWriter.WriteFile(outputFileName, results);
    }

    /// <summary>
    /// Generates a file from a Liquid template using a strongly-typed model.
    /// </summary>
    /// <typeparam name="T">The model type implementing ITemplateModel.</typeparam>
    /// <param name="templateName">The name of the template file.</param>
    /// <param name="outputFileName">The full path where the generated file should be written.</param>
    /// <param name="model">The model instance to use for template rendering.</param>
    public async Task GenerateFile<T>(string templateName, string outputFileName, T model) where T : ITemplateModel
    {
        await this.GenerateFile(templateName, outputFileName, await model.ToTemplateContext());
    }

    /// <summary>
    /// Adds a key-value pair to the global template context that will be available in all template renderings.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    public void AddToGlobalContext(string key, object value)
    {
        this.GlobalContext.TryAdd(key, value);
    }

    public void AddToGlobalContext(Dictionary<string, object> dict)
    {
        foreach (var (key, value) in dict)
        {
            this.AddToGlobalContext(key, value);
        }
    }

    /// <summary>
    /// Validates that the template path does not contain path traversal sequences or absolute paths.
    /// This prevents malicious models from reading arbitrary files outside the template directory.
    /// </summary>
    /// <param name="templateName">The template file name to validate.</param>
    /// <exception cref="InvalidTemplateDirectoryException">Thrown if the template path is invalid or escapes the template directory.</exception>
    private void ValidateTemplatePath(string templateName)
    {
        // Check for null/empty
        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("Template name cannot be null or empty", nameof(templateName));
        }

        // Check for path traversal characters
        if (templateName.Contains(".."))
        {
            var ex = new InvalidTemplateDirectoryException(templateName, this.TemplatePath);
            this.Logger.LogError(ex, "Template path contains path traversal sequence (..): {TemplateName}", templateName);
            throw ex;
        }

        // Check for tilde (home directory reference)
        if (templateName.Contains("~"))
        {
            var ex = new InvalidTemplateDirectoryException(templateName, this.TemplatePath);
            this.Logger.LogError(ex, "Template path contains home directory reference (~): {TemplateName}", templateName);
            throw ex;
        }

        // Check if rooted path (absolute path)
        if (Path.IsPathRooted(templateName))
        {
            var ex = new InvalidTemplateDirectoryException(templateName, this.TemplatePath);
            this.Logger.LogError(ex, "Template path is an absolute path: {TemplateName}", templateName);
            throw ex;
        }

        // Build full path and ensure it's within template directory
        var fullPath = Path.GetFullPath(Path.Combine(this.TemplatePath, templateName));
        var baseDirectory = Path.GetFullPath(this.TemplatePath);

        // Case-insensitive on Windows, sensitive on Linux
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        // Ensure the resolved path is within the base directory
        // Add path separator check to prevent directory name prefix matching
        if (!fullPath.StartsWith(baseDirectory + Path.DirectorySeparatorChar, comparison) &&
            !fullPath.Equals(baseDirectory, comparison))
        {
            var ex = new InvalidTemplateDirectoryException(templateName, this.TemplatePath);
            this.Logger.LogError(ex, "Template path escapes template directory: {TemplateName}", templateName);
            throw ex;
        }
    }

    private static void ValidateTemplateOutput(string templateOutput, Dictionary<string,object> model, string outputPath, string templateName)
    {
        if (templateOutput == "Liquid error: Error - This liquid context does not allow includes")
        {
            throw new TemplateRenderingException("This liquid context does not allow includes", model, outputPath, templateName);
        }
    }

    // ReSharper disable once InconsistentNaming
    private readonly Dictionary<string, object> GlobalContext;
    
    private IGeneralFileWriter FileWriter { get; }
    
    private string TemplatePath { get; }
    
    private ILogger<TemplateRenderer> Logger { get; }
}