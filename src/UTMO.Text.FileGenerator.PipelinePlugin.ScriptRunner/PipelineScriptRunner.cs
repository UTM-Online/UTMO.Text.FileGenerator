namespace UTMO.Text.FileGenerator.PipelinePlugin.ScriptRunner;

using System.Management.Automation;
using System.Reflection;

using Abstract;

public class PipelineScriptRunner : IPipelinePlugin
{
    public PipelineScriptRunner(IGeneralFileWriter writer)
    {
        this.Writer = writer;
    }
    
    public string ScriptName { get; internal set; }
    
    public Action<ITemplateGenerationEnvironment,Dictionary<string,object>> ScriptParametersBuilder { get; internal set; }
    
    private Dictionary<string,object> ScriptParameters { get; } = new();
    
    public void ProcessPlugin(ITemplateGenerationEnvironment environment)
    {
        var resourceName = Assembly.GetCallingAssembly().GetManifestResourceNames().Single(x => x.EndsWith(this.ScriptName));
        using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream ?? throw new InvalidOperationException());
        var script = reader.ReadToEnd();
        this.ScriptParametersBuilder.Invoke(environment, this.ScriptParameters);
        var shell = PowerShell.Create().AddScript(script);

        if (this.ScriptParameters.Any())
        {
            shell.AddParameters(this.ScriptParameters);
        }

        shell.Invoke();
    }

    public IGeneralFileWriter Writer { get; init; }
}