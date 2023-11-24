namespace UTMO.Text.FileGenerator.Exceptions;

using Abstract;

using CommandLine;

public class TemplateRenderingException : ApplicationException
{
    public TemplateRenderingException(string message, Dictionary<string,object> model, string outputPath) : base($"A exception occured rending template {model["TemplatePath"]} of type {model["ResourceTypeName"]} with name {model["ResourceName"]} to {outputPath} with message {message}")
    {
        this.TemplateName = model["TemplatePath"].Cast<string>();
        this.OutputFileName = outputPath;
        this.ResourceTypeName = model["ResourceTypeName"].Cast<string>();
        this.ResourceName = model["ResourceName"].Cast<string>();
    }
    
    public string TemplateName { get; set; }
    
    public string OutputFileName { get; set; }
    
    public string ResourceTypeName { get; set; }
    
    public string ResourceName { get; set; }
}