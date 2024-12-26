namespace UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

public class InvalidTemplateDirectoryException(string templateName, string templateDirectory) : ApplicationException($"The template directory \"{templateDirectory}\" does not exist.")
{
    public string TemplateDirectory { get; } = templateDirectory;
    public string TemplateName { get; } = templateName;
}