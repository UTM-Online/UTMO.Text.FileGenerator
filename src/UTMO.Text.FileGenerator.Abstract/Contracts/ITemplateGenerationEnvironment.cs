// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Abstract
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="ITemplateGenerationEnvironment.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract
{
    /// <summary>
    ///     Interface ITemplateGenerationEnvironment
    /// </summary>
    public interface ITemplateGenerationEnvironment : ITemplateGenerationEnvironmentLite
    {
        /// <summary>
        /// The Plugin Manager.
        /// </summary>
        IPluginManager PluginManager { get; }

        /// <summary>Gets the resources to be processed by the generation environment.</summary>
        /// <value>The resources.</value>
        IReadOnlyList<ITemplateModel> Resources { get; }

        /// <summary>
        /// Gets the command line options for the generation environment.
        /// </summary>
        /// <typeparam name="T">The type of the command line options.</typeparam>
        /// <returns>The command line options.</returns>
        T GetCommandLineOptions<T>() where T : class;
    }
}