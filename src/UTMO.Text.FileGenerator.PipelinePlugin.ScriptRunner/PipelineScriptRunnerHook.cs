namespace UTMO.Text.FileGenerator.PipelinePlugin.ScriptRunner;

using Abstract;

public static class PipelineScriptRunnerHook
{
    public static IRegisterPluginManager UsePipelineScriptRunner(this IRegisterPluginManager pluginManager, string scriptName, Action<ITemplateGenerationEnvironment, Dictionary<string, object>> parameterBuilder)
    {
        if (pluginManager is IPluginManager pm)
        {
            var plugin = new PipelineScriptRunner(pm.Resolve<IGeneralFileWriter>())
                             {
                                 ScriptName = scriptName,
                                 ScriptParametersBuilder = parameterBuilder
                             };

            pluginManager.RegisterAfterPipelinePlugin(plugin);
        }

        return pluginManager;
    }
}