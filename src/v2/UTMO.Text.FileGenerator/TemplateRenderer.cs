namespace UTMO.Text.FileGenerator;

using Abstract.Exceptions;
using DotLiquid;
using Extensions;
using Microsoft.Extensions.Logging;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

public class TemplateRenderer : ITemplateRenderer
{
    public TemplateRenderer(IGeneratorCliOptions options, IGeneralFileWriter fileWriter, ILogger<TemplateRenderer> logger)
    {
        this.FileWriter = fileWriter;
        this.GlobalContext = new Dictionary<string, object>();
        this.TemplatePath = options.TemplatePath;
        this.Logger = logger;
    }
    
    public async Task GenerateFile(string templateName, string outputFileName, Dictionary<string, object> dict)
    {
        if (!templateName.EndsWith(".liquid"))
        {
            templateName = string.Concat(templateName, ".liquid");
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

    public async Task GenerateFile<T>(string templateName, string outputFileName, T model) where T : ITemplateModel
    {
        await this.GenerateFile(templateName, outputFileName, await model.ToTemplateContext());
    }

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
}