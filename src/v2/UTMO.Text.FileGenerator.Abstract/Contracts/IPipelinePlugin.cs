namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    public interface IPipelinePlugin : IFileGeneratorPluginBase
    {
        /// <summary>
        /// Entry point for pipeline plugins.
        /// </summary>
        /// <param name="environment">The generation environment.</param>
        Task<bool> ProcessPlugin(ITemplateGenerationEnvironment environment);
        
        IGeneralFileWriter Writer { get; init; }
        
        ITemplateGenerationEnvironment Environment { get; init; }
        
        PluginPosition Position { get; }
    }
}