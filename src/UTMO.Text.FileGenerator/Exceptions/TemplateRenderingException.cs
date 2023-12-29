namespace UTMO.Text.FileGenerator.Exceptions;

using Abstract;

using CommandLine;

public class TemplateRenderingException : ApplicationException
{
    public TemplateRenderingException(string message, Dictionary<string,object> model, string outputPath) : base($"A exception occured rending template {model["TemplateName"]} to {outputPath} with message {message}")
    {
        this.TemplateName = model["TemplateName"].Cast<string>();
        this.OutputFileName = outputPath;
    }
    
    public string TemplateName { get; set; }
    
    public string OutputFileName { get; set; }
}