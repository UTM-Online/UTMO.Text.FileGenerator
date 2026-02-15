﻿// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Abstract
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/20/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/20/2023 2:29 PM
// // ***********************************************************************
// // <copyright file="IGeneralFileWriter.cs" company="Joshua S. Irwin">
// //     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    public interface IGeneralFileWriter
    {
        Task WriteFile(string fileName, string content, bool overwrite = false);

        Task WriteEmbeddedResource(string fileName, string outputPath, EmbeddedResourceType resourceType, Type resourceTypeObject);
    }
}