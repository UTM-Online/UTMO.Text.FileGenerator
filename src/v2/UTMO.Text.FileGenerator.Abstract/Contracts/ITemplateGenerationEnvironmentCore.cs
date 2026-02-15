﻿// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Abstract
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/08/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/08/2023 4:31 PM
// // ***********************************************************************
// // <copyright file="ITemplateGenerationEnvironmentLite.cs" company="Joshua S. Irwin">
// //     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    public interface ITemplateGenerationEnvironmentCore
    {
        /// <summary>
        ///     Adds a resource.
        /// </summary>
        /// <typeparam name="T">The type of the resource</typeparam>
        /// <param name="resource">The resource.</param>
        /// <returns>The generation environment.</returns>
        ITemplateGenerationEnvironment AddResource<T>(T resource) where T : ITemplateModel;

        /// <summary>
        ///     Adds a resource.
        /// </summary>
        /// <typeparam name="T">The type of the resource</typeparam>
        /// <returns>The generation environment.</returns>
        ITemplateGenerationEnvironment AddResource<T>() where T : ITemplateModel, new();

        /// <summary>
        ///     Adds a resource.
        /// </summary>
        /// <typeparam name="T">The type of the resources</typeparam>
        /// <param name="resources">The resources.</param>
        /// <returns>The generation environment.</returns>
        ITemplateGenerationEnvironment AddResources<T>(IEnumerable<T> resources) where T : ITemplateModel;

        /// <summary>Adds a key value pair to the environment context.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     <para>The generation environment.</para>
        /// </returns>
        ITemplateGenerationEnvironment AddEnvironmentContext(string key, object value);
    }
}