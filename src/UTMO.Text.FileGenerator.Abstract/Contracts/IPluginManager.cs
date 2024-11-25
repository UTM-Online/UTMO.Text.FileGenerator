namespace UTMO.Text.FileGenerator.Abstract
{
    public interface IPluginManager : IRegisterPluginManager, IPluginResolver
    {
        void InvokeBeforeRenderPlugins(ITemplateModel resource);
        
        void InvokeAfterRenderPlugins(ITemplateModel resource);

        void InvokeBeforePipelinePlugins(ITemplateGenerationEnvironment environment);
        
        void InvokeAfterPipelinePlugins(ITemplateGenerationEnvironment environment);
    }
}