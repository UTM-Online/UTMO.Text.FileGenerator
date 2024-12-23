// // ***********************************************************************
// // Assembly         : UTMO.Text.FileGenerator.Abstract
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/22/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/22/2023 12:43 PM
// // ***********************************************************************
// // <copyright file="IManifestProducer.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    public interface IManifestProducer
    {
        bool GenerateManifest { get; }
        
        Task<object?> ToManifest();
    }
}