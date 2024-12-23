// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Abstract
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/08/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/08/2023 4:14 PM
// // ***********************************************************************
// // <copyright file="ITemplateGenerationRenderer.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    public interface ITemplateGenerationRenderer
    {
        /// <summary> Generates the files from the environments resources. </summary>
        /// <param name="suppressFinalOutput">if set to <c>true</c> [suppress final log output].</param>
        Task Generate(bool suppressFinalOutput = false);
    }
}