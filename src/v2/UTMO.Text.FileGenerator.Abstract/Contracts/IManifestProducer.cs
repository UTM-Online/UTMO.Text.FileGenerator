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
    public interface IManifest
    {
    }

    public abstract class ManifestBase : IManifest
    {
    }
    
    public interface IManifestProducer
    {
        bool GenerateManifest { get; }
        
        Task<TManifest?> ToManifest<TManifest>() where TManifest : class, IManifest;
    }
}
