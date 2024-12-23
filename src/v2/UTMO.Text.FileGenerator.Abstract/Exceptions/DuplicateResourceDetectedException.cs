// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Core
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/22/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/22/2023 12:04 PM
// // ***********************************************************************
// // <copyright file="DuplicateResourceDetectedException.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Exceptions
{
    using Contracts;

    public class DuplicateResourceDetectedException : ApplicationException
    {
        public DuplicateResourceDetectedException(ITemplateModel model, string outputPath) : base($"Duplicate resource detected: Resource Type: \"{model.ResourceTypeName}\" Resource Name: \"{model.ResourceName}\" Target Output Path: \"{outputPath}\"")
        {
            this.ResourceTypeName = model.ResourceTypeName;
            this.ResourceName = model.ResourceName;
            this.TargetOutputPath = outputPath;
        }
        
        public string ResourceTypeName { get; set; }
        
        public string ResourceName { get; set; }
        
        public string TargetOutputPath { get; set; }
    }
}