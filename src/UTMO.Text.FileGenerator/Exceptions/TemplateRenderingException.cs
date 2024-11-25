namespace UTMO.Text.FileGenerator.Exceptions;

using CommandLine;

public class TemplateRenderingException : ApplicationException
{
    public TemplateRenderingException(string message, Dictionary<string,object> model, string outputPath, string templateName) : base($"A exception occured rending template {model["TemplateName"]} to {outputPath} with message {message}")
    {
        this.TemplateName = templateName;
        this.OutputFileName = outputPath;
        this.Model = model;
    }
    
    public string TemplateName { get; set; }
    
    public string OutputFileName { get; set; }
    
    public Dictionary<string, object> Model { get; set; }
}