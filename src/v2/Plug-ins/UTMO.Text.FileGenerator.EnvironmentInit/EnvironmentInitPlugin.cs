namespace UTMO.Text.FileGenerator.EnvironmentInit;

using Microsoft.Extensions.Logging;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;

public class EnvironmentInitPlugin : IPipelinePlugin
{
    public EnvironmentInitPlugin(ILogger<EnvironmentInitPlugin> logger)
    {
        this.Logger = logger;
    }
    
    public TimeSpan MaxRuntime => TimeSpan.FromMinutes(5);

    public async Task<bool> ProcessPlugin(ITemplateGenerationEnvironment environment)
    {
        try
        {
            this.Logger.LogInformation("Initializing Environment {EnvironmentName}", environment.EnvironmentName);
            // ReSharper disable once MethodHasAsyncOverload
            environment.Initialize();
            await environment.InitializeAsync();
            this.Logger.LogTrace("Environment Initialized");
            return true;
        }
        catch (Exception e)
        {
            this.Logger.LogError(e, "Error during Environment Initialization");
            return false;
        }
    }

    public IGeneralFileWriter? Writer { get; init; }

    public ITemplateGenerationEnvironment? Environment { get; init; }

    private ILogger<EnvironmentInitPlugin> Logger { get; }

    public PluginPosition Position => PluginPosition.Before;
}