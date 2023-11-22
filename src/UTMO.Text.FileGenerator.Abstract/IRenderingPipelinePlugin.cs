namespace UTMO.Text.FileGenerator.Abstract
{
    public interface IRenderingPipelinePlugin
    {
        /// <summary>
        /// Entry point for rendering pipeline plugins.
        /// </summary>
        /// <param name="model">The template instance being processed.</param>
        void HandleTemplate(ITemplateModel model);
        
        IGeneralFileWriter Writer { get; init; }
    }
}