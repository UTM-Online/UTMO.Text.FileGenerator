// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Core
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/08/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/08/2023 4:06 PM
// // ***********************************************************************
// // <copyright file="GenerationEnvironment.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator
{
    using System.Diagnostics.CodeAnalysis;
    using CommandLine;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Used by external projects, justification approved.")]
    public class GenerationEnvironment
    {
        private GenerationEnvironment(GenerationEnvironmentBase environment)
        {
            this.Environment = environment;
        }
        
        internal readonly GenerationEnvironmentBase Environment;
        
        /// <summary>
        /// Creates a new Generation Environment.
        /// </summary>
        /// <param name="templatePath">The path to the liquid template files.</param>
        /// <param name="environmentName">The name of the generation environment.</param>
        /// <returns>The generation environment.</returns>
        public static GenerationEnvironment Create(string templatePath, string environmentName)
        {
             var env =  new GenerationEnvironmentBase(templatePath)
            {
                Name = environmentName,
                OutputPath = Path.Join(Directory.GetCurrentDirectory(), "FileGeneratorOut", environmentName)
            };
             
             return new GenerationEnvironment(env);
        }

        /// <summary>
        /// Creates a default Generation Environment.
        /// </summary>
        /// <returns>The generation environment.</returns>
        public static GenerationEnvironment CreateDefault()
        {
            return Create($"{Directory.GetCurrentDirectory()}\\Templates", "Default");
        }
        
        /// <summary>
        /// Links a Command Line Parser class to the Generation Environment.
        /// </summary>
        /// <param name="args">The commands line arguments.</param>
        /// <typeparam name="T">The type of the Command Line Options class being parsed.</typeparam>
        /// <returns>The generation environment.</returns>
        public GenerationEnvironment ParseCommandLineOptions<T>(string[] args)
        {
            this.Environment.CommandLineOptions = Parser.Default.ParseArguments<T>(args).Value;
            return this;
        }

        public GenerationEnvironment DisableManifestGeneration()
        {
            this.Environment.GenerateManifest = false;
            return this;
        }
    }
}