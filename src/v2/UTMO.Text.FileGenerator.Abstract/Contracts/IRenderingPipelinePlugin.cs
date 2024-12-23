namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    public interface IRenderingPipelinePlugin : IFileGeneratorPluginBase
    {
        /// <summary>
        /// Entry point for rendering pipeline plugins.
        /// </summary>
        /// <param name="model">The template instance being processed.</param>
        Task HandleTemplate(ITemplateModel model);
        
        IGeneralFileWriter Writer { get; init; }
        
        ITemplateGenerationEnvironment Environment { get; init; }
        
        PluginPosition Position { get; }
    }
}