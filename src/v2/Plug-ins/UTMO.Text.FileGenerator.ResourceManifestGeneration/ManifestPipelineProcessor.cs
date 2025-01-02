// // ***********************************************************************
// // Assembly         : UTMO.Text.FileGenerator
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/22/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/22/2023 2:04 PM
// // ***********************************************************************
// // <copyright file="ManifestPipelineProcessor.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

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
        public ManifestPipelineProcessor(IGeneralFileWriter writer, ILogger<ManifestPipelineProcessor> logger)
        {
            this.Writer = writer;
            this.Logger = logger;
        }

        public async Task ProcessPlugin(ITemplateGenerationEnvironment environment)
        {
            if (!environment.GeneratorOptions.GenerateManifest)
            {
                this.Logger.LogInformation(SkippingManifestGeneration, environment.EnvironmentName);
                return;
            }

            this.Logger.LogInformation("Generating Manifest References");
            var resourceManifests  = new List<(string ResourceTypeName, string ResourceName, IManifestProducer producer)>();
            var manifestOutputPath = Path.Join(environment.GeneratorOptions.OutputPath, "Manifests");

            foreach (var resource in environment.Resources)
            {
                await resource.GenerateResourceManifest(resourceManifests, this.Logger);
            }
            
            var manifestGroups = resourceManifests.GroupBy(a => a.ResourceTypeName).ToList();

            foreach (var manifest in manifestGroups)
            {
                var manifestsToWriteTasks = manifest.DistinctBy(a => new { a.ResourceName, a.ResourceTypeName }).Select(a => a.producer.ToManifest()).ToList();
                var manifestsToWrite      = await Task.WhenAll(manifestsToWriteTasks);
                var json                  = JsonConvert.SerializeObject(manifestsToWrite, Formatting.Indented);
                this.Logger.LogInformation(WritingManifestFile, manifest.Key, manifestOutputPath);
                await this.Writer.WriteFile($"{manifestOutputPath}\\{manifest.Key}.Manifest.json", json);
            }
            
            this.Logger.LogInformation("Manifest Generation Complete. Generated {CountOfManifests} manifests", manifestGroups.Count());
        }

        public IGeneralFileWriter Writer { get; init; }

        public ITemplateGenerationEnvironment Environment { get; init; } = null!;

        public PluginPosition Position => PluginPosition.Before;

        private ILogger<ManifestPipelineProcessor> Logger { get; }

        public TimeSpan MaxRuntime => TimeSpan.FromMinutes(10);
    }
}