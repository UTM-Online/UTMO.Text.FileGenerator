﻿// // ***********************************************************************
// // Assembly         : UTMO.Text.FileGenerator.Abstract
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/22/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/22/2023 12:43 PM
// // ***********************************************************************
// // <copyright file="IManifestProducer.cs" company="Joshua S. Irwin">
// //     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    /// <summary>
    /// Marker interface for manifest payload models produced by <see cref="IManifestProducer"/>.
    /// </summary>
    public interface IManifest
    {
    }

    /// <summary>
    /// Base manifest implementation for strongly typed manifest payloads.
    /// Inherit from this class for standard manifest models, or implement
    /// <see cref="IManifest"/> directly for custom scenarios.
    /// </summary>
    public abstract class ManifestBase : IManifest
    {
    }
    
    public interface IManifestProducer
    {
        bool GenerateManifest { get; }
        
        Task<TManifest?> ToManifest<TManifest>() where TManifest : class, IManifest;

        /// <summary>
        /// A stable, author-chosen identity for this producer's manifest. Manifest references
        /// declared via <c>new ManifestReference(subject, parentManifest)</c> resolve against
        /// this value, allowing a reference to be resolved from any location in the app without
        /// holding the referenced resource instance.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <see langword="null"/>, which means "no subject –
        /// only legacy (ResourceTypeName/ResourceName) resolution applies". Producers that want
        /// subject-based resolution should return a non-empty, unique value.
        /// </remarks>
        string? ManifestSubject => null;

        /// <summary>
        /// The optional subject of the parent manifest that scopes this manifest's
        /// <see cref="ManifestSubject"/>. When <see langword="null"/> the subject is resolved at
        /// the environment root scope. Use this to disambiguate subjects that are only unique
        /// within a parent.
        /// </summary>
        string? ParentManifestSubject => null;
    }
}
