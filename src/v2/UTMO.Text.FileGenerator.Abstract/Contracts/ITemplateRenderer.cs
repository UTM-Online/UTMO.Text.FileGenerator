﻿// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Abstract
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="ITemplateRenderer.cs" company="Joshua S. Irwin">
//     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    /// <summary>
    ///     Interface ITemplateRenderer
    /// </summary>
    public interface ITemplateRenderer
    {
        /// <summary>
        ///     Generates the file.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="outputFileName">Name of the output file.</param>
        /// <param name="dict">The rendering context.</param>
        Task GenerateFile(string templateName, string outputFileName, Dictionary<string, object> dict);

        /// <summary>
        ///     Generates the file.
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="outputFileName">Name of the output file.</param>
        /// <param name="model">The model.</param>
        Task GenerateFile<T>(string templateName, string outputFileName, T model) where T : ITemplateModel;

        /// <summary>
        ///     Adds to the global context.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void AddToGlobalContext(string key, object value);
        
        void AddToGlobalContext(Dictionary<string, object> dict);
    }
}