namespace UTMO.Text.FileGenerator.Extensions
{
    using Unity;
    using UTMO.Text.FileGenerator.Abstract;

    public static class PluginManagerExtensions
    {
        private static bool DiagnosticEnabled { get; set; }
        
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
        
        public static IRegisterPluginManager EnableDiagnostics(this IRegisterPluginManager pluginManager)
        {
            if (pluginManager is PluginManager pm && !DiagnosticEnabled)
            {
                pm.Container.AddExtension(new Diagnostic());
                DiagnosticEnabled = true;
            }
            
            return pluginManager;
        }
    }
}