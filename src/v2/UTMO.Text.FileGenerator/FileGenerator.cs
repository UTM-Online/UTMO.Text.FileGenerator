using Serilog.Core;

namespace UTMO.Text.FileGenerator;

using System.Diagnostics.CodeAnalysis;
using Abstract.Contracts;
using CommandLine;
using DotLiquid;
using DotLiquid.FileSystems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Models;
using ResourceManifestGeneration;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Utils;
using UTMO.Text.FileGenerator.EnvironmentInit;

/// <summary>
/// Factory and builder class for creating and configuring a file generator instance.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class FileGenerator
{
    // ReSharper disable once InconsistentNaming
    private static FileGenerator Generator = null!;

    // ReSharper disable once InconsistentNaming
    private readonly IHostBuilder HostBuilder;
    
    private bool CliOptionsConfigured { get; set; }
    
    private string[] CliArguments { get; }

    private bool UseAutoDiscovery { get; set; } = true;

    private FileGenerator(string[] cliArguments)
    {
        this.CliArguments = cliArguments;
        this.HostBuilder = Host.CreateDefaultBuilder();
    }

    /// <summary>
    /// Creates a new FileGenerator instance with default configuration.
    /// </summary>
    /// <param name="args">Command line arguments to parse.</param>
    /// <param name="logLevel">The minimum log level for Serilog. Default is Information.</param>
    /// <returns>A configured FileGenerator instance.</returns>
    public static FileGenerator Create(string[] args, LogEventLevel logLevel = LogEventLevel.Information)
    {
        Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .Enrich.WithExceptionDetails()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                    .MinimumLevel.Is(logLevel)
                    .CreateLogger();
        
        Log.Debug(@"Creating File Generator");
        Generator = new FileGenerator(args);

        Log.Debug(@"Configuring File Generator");
        Generator.HostBuilder.ConfigureServices(
            svc =>
            {
                svc.AddHostedService<FileGeneratorHost>();
                svc.AddTransient<ITemplateRenderer, TemplateRenderer>();
                svc.AddScoped<IGeneralFileWriter, DefaultFileWriter.DefaultFileWriter>();
                svc.AddSingleton<EnvironmentInitPlugin>();
                svc.AddSingleton<IPipelinePlugin, ManifestPipelineProcessor>();
                svc.AddFeatureManagement();
            });
        
        var featuresStream = typeof(FileGenerator).Assembly.GetManifestResourceStream("UTMO.Text.FileGenerator.FeatureFlights.manifest.json");

        Generator.HostBuilder.ConfigureHostConfiguration(builder =>
                                                         {
                                                             if (featuresStream is not null)
                                                             {
                                                                 builder.AddJsonStream(featuresStream);
                                                             }
                                                         });

        Log.Debug(@"Configuring File Generator Logging");
        Generator.ConfigureLogging(
            loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });
        
        Log.Information(@"File Generator Created");

        return Generator;
    }

    /// <summary>
    /// Configures additional services for dependency injection.
    /// </summary>
    /// <param name="configureServices">Action to configure services.</param>
    /// <returns>The FileGenerator instance for fluent chaining.</returns>
    public FileGenerator ConfigureServices(Action<IServiceCollection> configureServices)
    {
        Generator.HostBuilder.ConfigureServices(configureServices);
        return Generator;
    }

    public FileGenerator ConfigureHost(Action<IHostBuilder> configureHost)
    {
        configureHost(Generator.HostBuilder);
        return Generator;
    }

    public FileGenerator ConfigureLogging(Action<ILoggingBuilder> configureLogging)
    {
        Generator.HostBuilder.ConfigureLogging(configureLogging);
        return Generator;
    }

    /// <summary>
    /// Registers a pipeline plugin that runs before or after environment processing.
    /// </summary>
    /// <typeparam name="TPlugin">The plugin type implementing IPipelinePlugin.</typeparam>
    /// <returns>The FileGenerator instance for fluent chaining.</returns>
    public FileGenerator RegisterPipelinePlugin<TPlugin>() where TPlugin : IPipelinePlugin
    {
        Generator.HostBuilder.ConfigureServices(
            svc =>
            {
                svc.AddScoped(typeof(IPipelinePlugin), typeof(TPlugin));
            });
        return Generator;
    }

    /// <summary>
    /// Registers a rendering pipeline plugin that runs before or after template rendering.
    /// </summary>
    /// <typeparam name="TPlugin">The plugin type implementing IRenderingPipelinePlugin.</typeparam>
    /// <returns>The FileGenerator instance for fluent chaining.</returns>
    public FileGenerator RegisterRendererPlugin<TPlugin>() where TPlugin : IRenderingPipelinePlugin
    {
        Generator.HostBuilder.ConfigureServices(
            svc =>
            {
                svc.AddScoped(typeof(IRenderingPipelinePlugin), typeof(TPlugin));
            });
        return Generator;
    }

    /// <summary>
    /// Registers a specific environment for template generation. Disables auto-discovery when called.
    /// </summary>
    /// <typeparam name="TEnvironment">The environment type implementing ITemplateGenerationEnvironment.</typeparam>
    /// <returns>The FileGenerator instance for fluent chaining.</returns>
    public FileGenerator UseEnvironment<TEnvironment>() where TEnvironment : ITemplateGenerationEnvironment
    {
        this.UseAutoDiscovery = false;
        Generator.HostBuilder.ConfigureServices(
            svc =>
            {
                svc.AddSingleton(typeof(ITemplateGenerationEnvironment), typeof(TEnvironment));
            });
        return Generator;
    }
    
    public FileGenerator UseEnvironment<TEnvironment>(TEnvironment environment) where TEnvironment : ITemplateGenerationEnvironment
    {
        this.UseAutoDiscovery = false;
        Generator.HostBuilder.ConfigureServices(
            svc =>
            {
                svc.AddSingleton(typeof(ITemplateGenerationEnvironment), environment);
            });
        return Generator;
    }

    /// <summary>
    /// Registers a custom CLI options class for parsing command line arguments.
    /// </summary>
    /// <typeparam name="T">The CLI options type implementing IGeneratorCliOptions.</typeparam>
    /// <returns>The FileGenerator instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CLI options cannot be parsed.</exception>
    public FileGenerator RegisterCustomCliOptions<T>() where T : class, IGeneratorCliOptions
    {
        var options = Parser.Default.ParseArguments<T>(this.CliArguments);

        if (options is null || options.Errors.Any())
        {
            var errorMessages = options?.Errors.Select(e => e.ToString()) ?? new[] { "Unknown parsing error" };
            var errorDetails = string.Join(Environment.NewLine, errorMessages);
            throw new InvalidOperationException($"Unable to parse CLI options. Errors:{Environment.NewLine}{errorDetails}");
        }
        
        var parsedOptions = options.Value;
        
        Template.FileSystem = new LocalFileSystem(parsedOptions.TemplatePath);
        
        this.HostBuilder.ConfigureServices(svc => svc.AddSingleton<IGeneratorCliOptions>(parsedOptions));
        this.CliOptionsConfigured = true;
        return this;
    }

    /// <summary>
    /// Starts the file generation process. This will discover environments (if auto-discovery is enabled),
    /// configure services, and run the hosted service.
    /// </summary>
    public void Run()
    {
        Log.Debug(@"Preparing to run the File Generator");
        if (this.UseAutoDiscovery)
        {
            // Search for all implementations of ITemplateGenerationEnvironment and store them in a list
            var assemblies      = AppDomain.CurrentDomain.GetAssemblies();
            var implementations = InterfaceImplementationsFinder.FindImplementations<ITemplateGenerationEnvironment>(assemblies).DistinctBy(a => a.FullName).ToList();

            foreach (var implementation in implementations)
            {
                Generator.HostBuilder.ConfigureServices(
                                                        svc =>
                                                        {
                                                            svc.AddSingleton(typeof(ITemplateGenerationEnvironment), implementation);
                                                        });
            }
        }
        
        if (!this.CliOptionsConfigured)
        {
            var options = Parser.Default.ParseArguments<GeneratorCliOptions>(this.CliArguments);
            Template.FileSystem = new LocalFileSystem(options.Value.TemplatePath);
            this.HostBuilder.ConfigureServices(svc => svc.AddSingleton<IGeneratorCliOptions>(options.Value));
        }

        Log.Debug(@"Running the File Generator");
        
        Generator.HostBuilder.Build().Run();
    }
}