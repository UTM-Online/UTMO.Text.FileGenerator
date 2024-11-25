// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10/09/2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-13-2023
// ***********************************************************************
// <copyright file="GenerationEnvironmentBase.cs" company="Microsoft Corp">
//         Copyright (c) Microsoft Corporation. All rights reserved.

// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using UTMO.Text.FileGenerator.Abstract;
using UTMO.Text.FileGenerator.Exceptions;
using UTMO.Text.FileGenerator.Extensions;
using UTMO.Text.FileGenerator.Logging;
using UTMO.Text.FileGenerator.Manifests;
using UTMO.Text.FileGenerator.Messages;
using UTMO.Text.FileGenerator.Messages;
using UTMO.Text.FileGenerator.Writer;

/// <summary>
///     The generation environment base class.
///     Implements the <see cref="ITemplateGenerationEnvironment" />
/// </summary>
/// <seealso cref="ITemplateGenerationEnvironment" />
[SuppressMessage("ReSharper", "ExceptionNotThrown", Justification = "suppression Approved.")]
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class GenerationEnvironmentBase : ITemplateGenerationEnvironment, ITemplateGenerationRenderer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GenerationEnvironmentBase" /> class.
    /// </summary>
    /// <param name="templatePath">The template path.</param>
    protected internal GenerationEnvironmentBase(string templatePath)
    {
        this.ResourcesInternal = new List<ITemplateModel>();
        this.Name = "Default";
        this.PluginManager.RegisterFileWriter();
        this.PluginManager.RegisterBeforePipelinePlugin<ManifestPipelineProcessor>();
        this.PluginManager.RegisterDependency<ITemplateGenerationEnvironment>(this);
        this.Renderer = new TemplateRenderer(templatePath.NormalizePath(), this.PluginManager.Resolve<IGeneralFileWriter>());
    }

    /// <summary>
    ///     Gets the resources.
    /// </summary>
    /// <value>The resources.</value>
    private List<ITemplateModel> ResourcesInternal { get; }

    /// <summary>
    ///     Gets the renderer.
    /// </summary>
    /// <value>The template rendering engine.</value>
    // ReSharper disable once MemberCanBePrivate.Global
    protected ITemplateRenderer Renderer { get; }

    internal object? CommandLineOptions { get; set; }

    /// <summary>
    ///     The name of the generation environment.
    /// </summary>
    /// <value>The environment name.</value>
    public string Name { get; internal set; }

    /// <summary>
    ///     Gets the output path.
    /// </summary>
    /// <value>The output path.</value>
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
    public virtual string OutputPath { get; internal set; } = "Default";

    /// <summary>
    ///     Instructs the generator to create resource manifests or not.
    /// </summary>
    public bool GenerateManifest { get; internal set; } = true;

    /// <summary>Gets the resources.</summary>
    /// <value>The resources.</value>
    public IReadOnlyList<ITemplateModel> Resources => this.ResourcesInternal;

    public T GetCommandLineOptions<T>() where T : class
    {
        if (this.CommandLineOptions == null)
        {
            throw new InvalidOperationException("The command line options have not been set.");
        }

        try
        {
            return (T) this.CommandLineOptions;
        }
        catch (InvalidCastException)
        {
            throw new InvalidOperationException($"The command line options are not of type {typeof(T).Name}");
        }
    }

    /// <summary>Gets the plugin manager.</summary>
    /// <value>The plugin manager.</value>
    public IPluginManager PluginManager => UTMO.Text.FileGenerator.PluginManager.Instance;

    /// <summary>
    ///     Adds a resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource</typeparam>
    /// <param name="resource">The resource.</param>
    /// <returns>The generation environment.</returns>
    public ITemplateGenerationEnvironment AddResource<T>(T resource) where T : ITemplateModel
    {
        this.ValidateResource(resource);
        this.ResourcesInternal.Add(resource);
        return this;
    }

    /// <summary>
    ///     Adds a resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource</typeparam>
    /// <returns>The generation environment.</returns>
    public ITemplateGenerationEnvironment AddResource<T>() where T : ITemplateModel, new()
    {
        var resource = new T();
        this.ValidateResource(resource);
        this.ResourcesInternal.Add(resource);
        return this;
    }

    /// <summary>
    ///     Adds a resource.
    /// </summary>
    /// <typeparam name="T">The type of the resources</typeparam>
    /// <param name="resources">The resources.</param>
    /// <returns>The generation environment.</returns>
    public ITemplateGenerationEnvironment AddResources<T>(IEnumerable<T> resources) where T : ITemplateModel
    {
        foreach (var resource in resources)
        {
            this.ValidateResource(resource);
            this.ResourcesInternal.Add(resource);
        }

        return this;
    }

    /// <summary>
    ///     Adds a key value pair to the environment context.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The generation environment.</returns>
    public ITemplateGenerationEnvironment AddEnvironmentContext(string key, object value)
    {
        this.Renderer.AddToGlobalContext(key, value);
        return this;
    }

    /// <summary>
    ///     Generates the files from the environments resources.
    /// </summary>
    /// <param name="suppressFinalOutput">if set to <c>true</c> [suppress final log output].</param>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
    public virtual void Generate(bool suppressFinalOutput = false)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        var logger = this.PluginManager.ResolveLogger();

        this.PluginManager.InvokeBeforePipelinePlugins(this);

        foreach (var item in this.ResourcesInternal)
        {
            this.PluginManager.InvokeBeforeRenderPlugins(item);
            logger.Information(LogMessage.BeginResourceGeneration, item.ResourceTypeName, item.ResourceName, item.TemplatePath);
            var timer = new Stopwatch();
            timer.Start();
            this.Renderer.GenerateFile(item.TemplatePath, item.ProduceOutputPath(this.OutputPath), item);
            timer.Stop();
            logger.Verbose(LogMessage.TotalGenerationTime, timer.Elapsed.TotalMilliseconds, timer.Elapsed.TotalSeconds);
            this.PluginManager.InvokeAfterRenderPlugins(item);
        }

        this.PluginManager.InvokeAfterPipelinePlugins(this);

        if (!suppressFinalOutput)
        {
            
            logger.Information(LogMessage.GenerationCompleate, Path.GetFullPath(this.OutputPath));
        }
    }

    private void ValidateResource<T>(T resource) where T : ITemplateModel
    {
        var resourceOutputPath = resource.ProduceOutputPath(this.OutputPath);
        if (this.ResourcesInternal.Any(x => x.ProduceOutputPath(this.OutputPath) == resourceOutputPath))
        {
            throw new DuplicateResourceDetectedException(resource, resourceOutputPath);
        }
    }
}