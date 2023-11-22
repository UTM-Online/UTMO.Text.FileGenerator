// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Core
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/08/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/08/2023 10:24 AM
// // ***********************************************************************
// // <copyright file="GenerationEnvironmentExtensions.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Extensions
{
    using System.Diagnostics.CodeAnalysis;
    using Abstract;

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "External Facing API, suppression approved.")]
    public static class GenerationEnvironmentExtensions
    {
        /// <summary>
        /// Configures the Generation Environment using the provided action.
        /// </summary>
        /// <param name="genEnv">The Generation Environment.</param>
        /// <param name="configure">The user supplied action that configures the Generation Environment.</param>
        /// <returns>The Template Generation Renderer.</returns>
        public static ITemplateGenerationRenderer Configure(this GenerationEnvironment genEnv, Action<ITemplateGenerationEnvironmentLite, IPluginManager> configure)
        {
            configure.Invoke(genEnv.Environment, genEnv.Environment.PluginManager);
            return genEnv.Environment;
        }
        
        /// <summary>
        /// Configures the generation environment using the provided startup class.
        /// </summary>
        /// <param name="genEnv">The Generation Environment.</param>
        /// <typeparam name="T">The Startup Class that builds the generation environment.</typeparam>
        /// <returns>The Template Generation Renderer.</returns>
        public static ITemplateGenerationRenderer UsingStartUp<T>(this GenerationEnvironment genEnv) where T : class, IGeneratorStartup
        {
            var startup = Activator.CreateInstance<T>();
            startup.ConfigureEnvironment(genEnv.Environment);
            startup.ConfigurePlugins(genEnv.Environment.PluginManager);
            return genEnv.Environment;
        }
        
        /// <summary>
        /// Overrides the configured Output Path for generated files.
        /// </summary>
        /// <param name="genEnv">The generation environment.</param>
        /// <param name="outputPath">The path to output the generated files to.</param>
        /// <returns>The generation environment.</returns>
        public static GenerationEnvironment OverrideOutputPath(this GenerationEnvironment genEnv, string outputPath)
        {
            genEnv.Environment.OutputPath = outputPath;
            return genEnv;
        }
        
        /// <summary>
        /// Overrides the configured environment name.
        /// </summary>
        /// <param name="genEnv">The generation environment.</param>
        /// <param name="name">The name of the environment.</param>
        /// <returns>The generation environment.</returns>
        public static GenerationEnvironment OverrideName(this GenerationEnvironment genEnv, string name)
        {
            genEnv.Environment.Name = name;
            return genEnv;
        }

        /// <summary>
        /// Overrides the configured Output Path for generated files.
        /// </summary>
        /// <param name="genEnv">The generation environment.</param>
        /// <param name="nameBuilder">The path to output the generated files to.</param>
        /// <returns>The generation environment.</returns>
        public static GenerationEnvironment OverrideOutputPath(this GenerationEnvironment genEnv, Func<ITemplateGenerationEnvironment, string> nameBuilder)
        {
            genEnv.Environment.OutputPath = nameBuilder.Invoke(genEnv.Environment);
            return genEnv;
        }
    }
}