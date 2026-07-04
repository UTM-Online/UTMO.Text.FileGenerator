﻿// // ***********************************************************************
// // Assembly         : UTMO.Text.FileGenerator
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/22/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/22/2023 2:04 PM
// // ***********************************************************************
// // <copyright file="ManifestPipelineProcessor.cs" company="Joshua S. Irwin">
// //     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

using Microsoft.FeatureManagement;

namespace UTMO.Text.FileGenerator.ResourceManifestGeneration
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Versioning;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using UTMO.Text.FileGenerator.Abstract;
    using UTMO.Text.FileGenerator.Abstract.Contracts;
    using static LogMessage;
    using Formatting = Newtonsoft.Json.Formatting;

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    [SuppressMessage("Usage", "CA2254:Template should be a static expression")]
    public class ManifestPipelineProcessor : IPipelinePlugin
    {
        public ManifestPipelineProcessor(IGeneralFileWriter writer, ILogger<ManifestPipelineProcessor> logger, IFeatureManager fm)
        {
            this.Writer = writer;
            this.Logger = logger;
            this.FeatureManager = fm;
        }

        public async Task<bool> ProcessPlugin(ITemplateGenerationEnvironment environment)
        {
            try
            {
                if (environment.GeneratorOptions is { GenerateManifest: false, GenerateManifestsOnly: false })
                {
                    this.Logger.LogInformation(SkippingManifestGeneration, environment.EnvironmentName);
                    return true;
                }

                this.Logger.LogInformation("Generating Manifest References");
                var resourceManifests  = new List<(string ResourceTypeName, string ResourceName, IManifestProducer producer)>();
                var manifestOutputPath = Path.Join(environment.GeneratorOptions.OutputPath, "Manifests");

                foreach (var resource in environment.Resources)
                {
                    await resource.GenerateResourceManifest(resourceManifests, this.Logger, this.FeatureManager);
                }
            
                var manifestGroups = resourceManifests.GroupBy(a => a.ResourceTypeName).ToList();

                foreach (var manifest in manifestGroups)
                {
                    var manifestsToWriteTasks = manifest.DistinctBy(a => new { a.ResourceName, a.ResourceTypeName }).OrderBy(a => a.ResourceName).Select(a => a.producer.ToManifest<IManifest>()).ToList();
                    var manifestsToWrite      = await Task.WhenAll(manifestsToWriteTasks);
                    var json                  = JsonConvert.SerializeObject(manifestsToWrite, Formatting.Indented);
                    this.Logger.LogInformation(WritingManifestFile, manifest.Key, manifestOutputPath);
                    
                    if (this.Writer is null)
                    {
                        this.Logger.LogError("No writer found for manifest output");
                        return false;
                    }
                    
                    await this.Writer.WriteFile($"{manifestOutputPath}\\{manifest.Key}.Manifest.json", json, environment.GeneratorOptions.AllowOverwrite);
                }
            
                this.Logger.LogInformation("Manifest Generation Complete. Generated {CountOfManifests} manifests", manifestGroups.Count());
                return true;
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Error during Manifest Generation");
                return false;
            }
        }

        public IGeneralFileWriter? Writer { get; init; }

        public ITemplateGenerationEnvironment? Environment { get; init; } = null!;

        public PluginPosition Position => PluginPosition.Before;

        private ILogger<ManifestPipelineProcessor> Logger { get; }

        public TimeSpan MaxRuntime => TimeSpan.FromMinutes(10);
        
        public bool RequiresGeneration => false;
        
        private IFeatureManager FeatureManager { get; }
    }
}