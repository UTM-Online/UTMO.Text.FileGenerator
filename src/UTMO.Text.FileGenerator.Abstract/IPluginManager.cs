namespace UTMO.Text.FileGenerator.Abstract
{
    public interface IPluginManager : IRegisterPluginManager
    {
        void InvokeBeforeRenderPlugins(ITemplateModel resource);
        
        void InvokeAfterRenderPlugins(ITemplateModel resource);

        void InvokeBeforePipelinePlugins(ITemplateGenerationEnvironment environment);
        
        void InvokeAfterPipelinePlugins(ITemplateGenerationEnvironment environment);
        
        T Resolve<T>();
    }
}