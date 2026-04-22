namespace UTMO.Text.FileGenerator;

using Abstract.Exceptions;
using Constants;
using DotLiquid;
using Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

/// <summary>
/// Renders Liquid templates to generate text files with support for global context injection.
/// </summary>
public class TemplateRenderer : ITemplateRenderer
{
    /// <summary>Default render timeout in seconds when not specified in configuration.</summary>
    private const int DefaultRenderTimeoutSeconds = 30;

    /// <summary>Default maximum output size in bytes (10 MB) when not specified in configuration.</summary>
    private const int DefaultMaxOutputSizeBytes = 10 * 1024 * 1024;

    public TemplateRenderer(IGeneratorCliOptions options, IGeneralFileWriter fileWriter, ILogger<TemplateRenderer> logger, IConfiguration configuration)
    {
        this.FileWriter = fileWriter;
        this.GlobalContext = new Dictionary<string, object>();
        this.TemplatePath = options.TemplatePath;
        this.Logger = logger;
        this.Configuration = configuration;
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
        
        var templateText   = await File.ReadAllTextAsync(templatePath);
        var parsedTemplate = Template.Parse(templateText);

        var timeoutSeconds = this.Configuration.GetValue<int?>("TemplateRendering:TimeoutSeconds") ?? DefaultRenderTimeoutSeconds;
        var maxOutputBytes = this.Configuration.GetValue<int?>("TemplateRendering:MaxOutputSizeBytes") ?? DefaultMaxOutputSizeBytes;

        string results;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            var renderTask = Task.Run(() => parsedTemplate.Render(Hash.FromDictionary(dict)));
            results = await renderTask.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            this.Logger.LogError("Template rendering for {TemplateName} exceeded timeout of {TimeoutSeconds} seconds", templateName, timeoutSeconds);
            throw new TemplateRenderingException($"Template rendering timeout after {timeoutSeconds}s", dict, outputFileName, templateName);
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

        if (results.Length > maxOutputBytes)
        {
            this.Logger.LogError("Template output for {TemplateName} exceeds maximum size of {MaxOutputSizeBytes} bytes (actual: {ActualSize} bytes)", templateName, maxOutputBytes, results.Length);
            throw new TemplateRenderingException($"Template output size {results.Length} bytes exceeds maximum allowed size of {maxOutputBytes} bytes", dict, outputFileName, templateName);
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

    private IConfiguration Configuration { get; }
}