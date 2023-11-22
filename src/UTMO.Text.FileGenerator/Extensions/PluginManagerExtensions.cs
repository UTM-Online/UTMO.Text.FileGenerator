namespace UTMO.Text.FileGenerator.Extensions
{
    using Abstract;

    public static class PluginManagerExtensions
    {
        public static IRegisterPluginManager RegisterDependency<T>(this IRegisterPluginManager pluginManager) where T : class
        {
            pluginManager.RegisterDependency(typeof(T));
            return pluginManager;
        }

        public static IRegisterPluginManager RegisterBeforeRenderPlugin<T>(this IRegisterPluginManager pluginManager) where T : IRenderingPipelinePlugin
        {
            pluginManager.RegisterBeforeRenderPlugin(typeof(T));
            return pluginManager;
        }

        public static IRegisterPluginManager RegisterAfterRenderPlugin<T>(this IRegisterPluginManager pluginManager) where T : IRenderingPipelinePlugin
        {
            pluginManager.RegisterAfterRenderPlugin(typeof(T));
            return pluginManager;
        }

        public static IRegisterPluginManager RegisterBeforePipelinePlugin<T>(this IRegisterPluginManager pluginManager) where T : IPipelinePlugin
        {
            pluginManager.RegisterBeforePipelinePlugin(typeof(T));
            return pluginManager;
        }

        public static IRegisterPluginManager RegisterAfterPipelinePlugin<T>(this IRegisterPluginManager pluginManager) where T : IPipelinePlugin
        {
            pluginManager.RegisterAfterPipelinePlugin(typeof(T));
            return pluginManager;
        }
    }
}