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

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    using UTMO.Text.FileGenerator.Abstract.Exceptions;

    /// <summary>
    ///     Interface ITemplateGenerationEnvironment
    /// </summary>
    public interface ITemplateGenerationEnvironment : ITemplateGenerationEnvironmentCore
    {
        /// <summary>
        /// Gets the name of the environment.
        /// </summary>
        /// <value>The name of the environment.</value>
        string EnvironmentName { get; }
        
        /// <summary>Gets the resources to be processed by the generation environment.</summary>
        /// <value>The resources.</value>
        IReadOnlyList<ITemplateModel> Resources { get; }
        
        /// <summary>
        /// Environment Constants injected into the template renderer as global variables.
        /// </summary>
        Dictionary<string,object> EnvironmentConstants { get; }
        
        /// <summary>
        /// Gets the environment configuration.
        /// </summary>
        /// <value>The environment configuration.</value>
        IGeneratorCliOptions GeneratorOptions { get; }

        /// <summary>
        /// Validates the generation environment.
        /// </summary>
        /// <returns>A task that represents the asynchronous validation operation. The task result contains a list of validation exceptions.</returns>
        Task<List<ValidationFailedException>> Validate();
        
        void Initialize();
        
        Task InitializeAsync();
    }
}