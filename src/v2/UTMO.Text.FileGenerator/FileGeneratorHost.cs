using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.Abstract.Exceptions;
using UTMO.Text.FileGenerator.Constants;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;
using UTMO.Text.FileGenerator.EnvironmentInit;
using UTMO.Text.FileGenerator.Logging;
using UTMO.Text.FileGenerator.Models;

namespace UTMO.Text.FileGenerator;

using static LogMessages;

[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
public class FileGeneratorHost : IHostedService
{
    private bool IsSuccessfulRun = true;

    public FileGeneratorHost(IServiceProvider provider, ILogger<FileGeneratorHost> logger, IGeneralFileWriter fileWriter, EnvironmentInitPlugin initPlugin)
    {
        this.Logger = logger;
        this.FileWriter = fileWriter;
        this.Provider = provider;
        this.Environments = provider.GetServices<ITemplateGenerationEnvironment>();
        this.BeforeRenderPlugins = provider.GetServices<IRenderingPipelinePlugin>().Where(x => x.Position == PluginPosition.Before);
        this.AfterRenderPlugins = provider.GetServices<IRenderingPipelinePlugin>().Where(x => x.Position == PluginPosition.After);
        this.BeforePipelinePlugins = provider.GetServices<IPipelinePlugin>().Where(x => x.Position == PluginPosition.Before);
        this.AfterPipelinePlugins = provider.GetServices<IPipelinePlugin>().Where(x => x.Position == PluginPosition.After);
        this.InitPlugin = initPlugin;
    }

    private IEnumerable<ITemplateGenerationEnvironment> Environments { get; }

    private ILogger<FileGeneratorHost> Logger { get; }

    private IServiceProvider Provider { get; }

    private IEnumerable<IRenderingPipelinePlugin> BeforeRenderPlugins { get; }

    private IEnumerable<IRenderingPipelinePlugin> AfterRenderPlugins { get; }

    private IEnumerable<IPipelinePlugin> BeforePipelinePlugins { get; }

    private IEnumerable<IPipelinePlugin> AfterPipelinePlugins { get; }

    private EnvironmentInitPlugin InitPlugin { get; }

    // TODO: Evaluate if this is needed
    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    private IGeneralFileWriter FileWriter { get; }

    private Dictionary<Type, int> ExceptionCounters { get; } = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.Logger.LogInformation("Initializing Environments");

            foreach (var env in this.Environments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await this.InitPlugin.ProcessPlugin(env).WaitAsync(cancellationToken);
            }

            this.Logger.LogInformation(@"Starting File Generation Validation");
            await this.Validate(cancellationToken).WaitAsync(cancellationToken);
            this.Logger.LogInformation(@"Executing File Generation Pipeline");

            if (!await this.RunBeforePipelinePlugins(cancellationToken).WaitAsync(cancellationToken))
            {
                this.IsSuccessfulRun = false;
            }

            var featureManager = this.Provider.GetService<IFeatureManager>();

            try
            {
                foreach (var env in this.Environments)
                {
                    this.Logger.LogInformation(@$"Starting generation for environment {env.EnvironmentName}");
                    cancellationToken.ThrowIfCancellationRequested();

                    if (await featureManager?.IsEnabledAsync(FeatureFlags.EnableParallelResourceRendering)!)
                    {
                        await Parallel.ForEachAsync(env.Resources.AsEnumerable(), cancellationToken, async (resource, token) =>
                                                                                                     {
                                                                                                         token.ThrowIfCancellationRequested();
                                                                                                         var renderer = GetTemplateRenderer(env);

                                                                                                         if (resource is TemplateResourceBase resourceBase)
                                                                                                         {
                                                                                                             resourceBase.FeatureManager = featureManager;
                                                                                                         }


                                                                                                         if (!await this.RunBeforeRenderPlugins(resource, token).WaitAsync(token))
                                                                                                         {
                                                                                                             this.IsSuccessfulRun = false;
                                                                                                         }

                                                                                                         this.Logger.LogInformation(BeginResourceGeneration, resource.ResourceTypeName, resource.ResourceName, resource.TemplatePath);
                                                                                                         var timer = new Stopwatch();
                                                                                                         timer.Start();
                                                                                                         await renderer.GenerateFile(resource.TemplatePath, resource.ProduceOutputPath(env.GeneratorOptions.OutputPath), resource).WaitAsync(token);
                                                                                                         timer.Stop();
                                                                                                         this.Logger.LogTrace(TotalGenerationTime, timer.Elapsed.TotalMilliseconds, timer.Elapsed.TotalSeconds);
                                                                                                         if (!await this.RunAfterRenderPlugins(resource, token).WaitAsync(token))
                                                                                                         {
                                                                                                             this.IsSuccessfulRun = false;
                                                                                                         }
                                                                                                     });
                    }
                    else
                    {
                        var renderer = GetTemplateRenderer(env);

                        foreach (var resource in env.Resources.Where(a => a.EnableGeneration))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (resource is TemplateResourceBase resourceBase)
                            {
                                resourceBase.FeatureManager = featureManager;
                            }

                            if (!await this.RunBeforeRenderPlugins(resource, cancellationToken).WaitAsync(cancellationToken))
                            {
                                this.IsSuccessfulRun = false;
                            }
                            this.Logger.LogInformation(BeginResourceGeneration, resource.ResourceTypeName, resource.ResourceName, resource.TemplatePath);
                            var timer = new Stopwatch();
                            timer.Start();
                            await renderer.GenerateFile(resource.TemplatePath, resource.ProduceOutputPath(env.GeneratorOptions.OutputPath), resource).WaitAsync(cancellationToken);
                            timer.Stop();
                            this.Logger.LogTrace(TotalGenerationTime, timer.Elapsed.TotalMilliseconds, timer.Elapsed.TotalSeconds);
                            if (!await this.RunAfterRenderPlugins(resource, cancellationToken).WaitAsync(cancellationToken))
                            {
                                this.IsSuccessfulRun = false;
                            }
                        }
                    }
                }
            }
            catch (InvalidTemplateDirectoryException)
            {
                this.ExceptionCounters[typeof(InvalidTemplateDirectoryException)] = this.ExceptionCounters.GetValueOrDefault(typeof(InvalidTemplateDirectoryException)) + 1;
            }

            if (!await this.RunAfterPipelinePlugins(cancellationToken).WaitAsync(cancellationToken))
            {
                this.IsSuccessfulRun = false;
            }

            if (this.IsSuccessfulRun)
            {
                this.Logger.LogInformation(@"File Generation Complete");
                Environment.Exit(0);
            }
            
            this.Logger.LogWarning(@"File Generation completed with errors");
            Environment.Exit(3);
        }
        catch (TaskCanceledException)
        {
            this.Logger.LogWarning(@"File Generation was cancelled");
            Environment.Exit(5);
        }
        catch (Exception ex)
        {
            this.Logger.LogCritical(ex, @"An unhandled exception occurred during file generation");
            Environment.Exit(1);
        }

        return;

        ITemplateRenderer GetTemplateRenderer(ITemplateGenerationEnvironment env)
        {
            var renderer = this.Provider.GetRequiredService<ITemplateRenderer>();

            renderer.AddToGlobalContext(env.EnvironmentConstants);
            return renderer;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.ExceptionCounters.Count != 0)
        {
            foreach (var ex in this.ExceptionCounters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.Logger.LogTrace("Encountered {ExceptionCount} {ExceptionType} exceptions", ex.Value, ex.Key.Name);
            }

            Environment.Exit(-315);
        }

        await Task.CompletedTask;
    }

    private async Task Validate(CancellationToken cancellationToken)
    {
        var validationExceptions = new List<ValidationFailedException>();

        foreach (var environment in this.Environments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var errors = await environment.Validate().WaitAsync(cancellationToken);
            validationExceptions.AddRange(errors);
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        validationExceptions = validationExceptions.Where(a => a != null).ToList();

        if (validationExceptions.Count != 0)
        {
            foreach (var exception in validationExceptions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.Logger.LogError(exception, "Validation failed for {ResourceName}", exception.ResourceName);
            }

            this.Logger.LogCritical(ValidationFailureEncountered, validationExceptions.Count);
            Environment.Exit(45);
        }
    }

    private async Task<bool> RunBeforePipelinePlugins(CancellationToken cancellationToken)
    {
        var result = true;

        foreach (var env in this.Environments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var plugin in this.BeforePipelinePlugins)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await plugin.ProcessPlugin(env).WaitAsync(cancellationToken))
                {
                    result = false;
                }
            }
        }

        return result;
    }

    private async Task<bool> RunAfterPipelinePlugins(CancellationToken cancellationToken)
    {
        var result = true;

        foreach (var env in this.Environments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var plugin in this.AfterPipelinePlugins)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await plugin.ProcessPlugin(env).WaitAsync(cancellationToken))
                {
                    result = false;
                }
            }
        }

        return result;
    }

    private async Task<bool> RunBeforeRenderPlugins(ITemplateModel model, CancellationToken cancellationToken)
    {
        var result = true;

        foreach (var plugin in this.BeforeRenderPlugins)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await plugin.HandleTemplate(model).WaitAsync(cancellationToken))
            {
                result = false;
            }
        }

        return result;
    }

    private async Task<bool> RunAfterRenderPlugins(ITemplateModel model, CancellationToken cancellationToken)
    {
        var result = true;

        foreach (var plugin in this.AfterRenderPlugins)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await plugin.HandleTemplate(model).WaitAsync(cancellationToken))
            {
                result = false;
            }
        }

        return result;
    }
}