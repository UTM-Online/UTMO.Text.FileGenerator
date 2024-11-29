// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Abstract
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/20/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/20/2023 2:29 PM
// // ***********************************************************************
// // <copyright file="IGeneralFileWriter.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract
{
    public interface IGeneralFileWriter
    {
        void WriteFile(string fileName, string content, bool overwrite = false);
        
        void WriteEmbeddedResource(string fileName, string outputPath, EmbeddedResourceType resourceType);
    }
}