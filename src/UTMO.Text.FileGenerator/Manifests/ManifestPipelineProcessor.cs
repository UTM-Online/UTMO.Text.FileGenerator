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

namespace UTMO.Text.FileGenerator.Manifests
{
    using Newtonsoft.Json;
    using UTMO.Text.FileGenerator.Abstract;

    public class ManifestPipelineProcessor : IPipelinePlugin
    {
        public ManifestPipelineProcessor(IGeneralFileWriter writer, ITemplateGenerationEnvironment environment)
        {
            this.Writer = writer;
            this.Environment = environment;
        }
        
        public void ProcessPlugin(ITemplateGenerationEnvironment environment)
        {
            if (!environment.GenerateManifest)
            {
                return;
            }
            
            Console.WriteLine("Generating Manifest References");
        
            var manifestList = new Dictionary<string, List<ITemplateModel>>();

            var resources = new List<ITemplateModel>();

            GenerationEnvironmentBase? env = null;

            if (environment is GenerationEnvironmentBase envb)
            {
                resources = envb.Resources.ToList();
                env = envb;
            }
        
            foreach(var resource in resources)
            {
                resource.GenerateResourceManifest(manifestList);
            }

            if (env == null)
            {
                return;
            }
        
            var manifestOutputPath = Path.Join(env.OutputPath, "Manifests");
            
            foreach (var manifest in manifestList)
            {
                var resourcesList = manifest.Value.Where(x => x.GenerateManifest).Select(x => x.ToManifest());

                if (!resourcesList.Any())
                {
                    continue;
                }
            
                var json = JsonConvert.SerializeObject(resourcesList, Formatting.Indented);
                Console.WriteLine($"Writing Manifest: \"{manifest.Key}.Manifest.json\" to \"{manifestOutputPath}\"");
                this.Writer.WriteFile($"{manifestOutputPath}\\{manifest.Key}.Manifest.json", json);
            }
        }

        public IGeneralFileWriter Writer { get; init; }

        public ITemplateGenerationEnvironment Environment { get; init; }

        public TimeSpan MaxRuntime => TimeSpan.FromMinutes(10);
    }
}