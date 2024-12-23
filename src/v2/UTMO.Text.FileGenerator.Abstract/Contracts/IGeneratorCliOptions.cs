namespace UTMO.Text.FileGenerator.Abstract.Contracts;

public interface IGeneratorCliOptions
{
    string OutputPath { get; }
    
    bool GenerateManifest { get; }
    
    string TemplatePath { get; }
}