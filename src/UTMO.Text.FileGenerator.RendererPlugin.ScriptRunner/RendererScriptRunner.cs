namespace UTMO.Text.FileGenerator.RendererPlugin.ScriptRunner;

using System.Management.Automation;
using System.Reflection;

using Abstract;

public class RendererScriptRunner : IRenderingPipelinePlugin
{
    public RendererScriptRunner(IGeneralFileWriter writer)
    {
        this.Writer = writer;
    }
    
    public string ScriptName { get; internal set; }
    
    public Func<ITemplateModel,Dictionary<string,object>> ScriptParameters { get; internal set; }

    public void HandleTemplate(ITemplateModel model)
    {
        var resourceName = Assembly.GetCallingAssembly().GetManifestResourceNames().Single(x => x.EndsWith(this.ScriptName));
        using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream ?? throw new InvalidOperationException());
        var script = reader.ReadToEnd();
        var scriptParameters = this.ScriptParameters.Invoke(model);
        var shell = PowerShell.Create().AddScript(script);

        if (scriptParameters.Any())
        {
            shell.AddParameters(scriptParameters);
        }

        shell.Invoke();
    }

    public IGeneralFileWriter Writer { get; init; }
}