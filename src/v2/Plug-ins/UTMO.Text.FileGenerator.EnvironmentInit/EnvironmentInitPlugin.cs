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

    public async Task ProcessPlugin(ITemplateGenerationEnvironment environment)
    {
        this.Logger.LogInformation("Initializing Environment {EnvironmentName}", environment.EnvironmentName);
        // ReSharper disable once MethodHasAsyncOverload
        environment.Initialize();
        await environment.InitializeAsync();
        this.Logger.LogTrace("Environment Initialized");
    }
    
    private ILogger<EnvironmentInitPlugin> Logger { get; }

    public IGeneralFileWriter Writer { get; init; } = null!;

    public ITemplateGenerationEnvironment Environment { get; init; } = null!;

    public PluginPosition Position => PluginPosition.Before;
}