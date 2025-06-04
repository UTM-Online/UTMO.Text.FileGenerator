namespace UTMO.Text.FileGenerator.Abstract.Contracts;

public interface IGeneratorCliOptions
{
    string OutputPath { get; }
    
    bool GenerateManifest { get; }
    
    bool AllowOverwrite { get; }
    
    string TemplatePath { get; }
}