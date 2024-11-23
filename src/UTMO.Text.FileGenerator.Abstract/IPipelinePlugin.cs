namespace UTMO.Text.FileGenerator.Abstract
{
    public interface IPipelinePlugin
    {
        /// <summary>
        /// Entry point for pipeline plugins.
        /// </summary>
        /// <param name="environment">The generation environment.</param>
        void ProcessPlugin(ITemplateGenerationEnvironment environment);
        
        IGeneralFileWriter Writer { get; init; }
        
        ITemplateGenerationEnvironment Environment { get; init; }
    }
}