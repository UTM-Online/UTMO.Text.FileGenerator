namespace UTMO.Text.FileGenerator.RendererPlugin.ScriptRunner;

using Abstract;

public static class RendererScriptRunnerHook
{
    public static IRegisterPluginManager UseRendererScriptRunner(this IRegisterPluginManager pluginManager, string scriptName, Func<ITemplateModel, Dictionary<string, object>> parameterBuilder)
    {
        if (pluginManager is IPluginManager pm)
        {
            var plugin = new RendererScriptRunner(pm.Resolve<IGeneralFileWriter>())
                             {
                                 ScriptName = scriptName,
                                 ScriptParameters = parameterBuilder
                             };

            pluginManager.RegisterAfterRenderPlugin(plugin);
        }

        return pluginManager;
    }
}