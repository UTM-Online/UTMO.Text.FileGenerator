namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    public interface IPluginManager : IRegisterPluginManager, IPluginResolver
    {
        Task InvokeBeforeRenderPlugins(ITemplateModel resource);
        
        Task InvokeAfterRenderPlugins(ITemplateModel resource);

        Task InvokeBeforePipelinePlugins(ITemplateGenerationEnvironment environment);
        
        Task InvokeAfterPipelinePlugins(ITemplateGenerationEnvironment environment);
    }
}