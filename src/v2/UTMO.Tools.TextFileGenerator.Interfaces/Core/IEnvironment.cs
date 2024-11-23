namespace UTMO.Tools.TextFileGenerator.Interfaces.Core
{
    public interface IEnvironment
    {
        string Alias { get; }
        
        bool IsEnabled { get; }
        
        IEnumerable<ITextTemplateModel> GenerateTemplates();
    }
}