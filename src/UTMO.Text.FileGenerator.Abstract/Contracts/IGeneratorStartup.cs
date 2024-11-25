// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Abstract
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/07/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/07/2023 4:50 PM
// // ***********************************************************************
// // <copyright file="IGeneratorStartup.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract
{
    public interface IGeneratorStartup
    {
        /// <summary>
        /// Configures the environment.
        /// </summary>
        /// <param name="environment">The Generation Environment.</param>
        void ConfigureEnvironment(ITemplateGenerationEnvironment environment);
        
        /// <summary>
        /// Configures the environment plugins.
        /// </summary>
        /// <param name="pluginManager">The environment plugin manager.</param>
        void ConfigurePlugins(IRegisterPluginManager pluginManager);
    }
}