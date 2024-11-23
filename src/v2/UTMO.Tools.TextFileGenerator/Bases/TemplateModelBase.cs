namespace UTMO.Tools.TextFileGenerator.Bases
{
    using DotLiquid;
    using UTMO.Tools.TextFileGenerator.Interfaces.Core;

    public abstract class TemplateModelBase : ITextTemplateModel
    {
        public abstract string ResourceTypeName { get; }
        
        public abstract string TemplatePath     { get; }
        
        public abstract string OutputExtension  { get; }
        
        public abstract string ResourceName     { get; }
        
        public abstract bool   GenerateFile     { get; }
        
        public abstract bool   GenerateManifest { get; }

        public abstract string ProduceOutputPath(string basePath);
        
        public abstract Drop ToDrop();
    }
}