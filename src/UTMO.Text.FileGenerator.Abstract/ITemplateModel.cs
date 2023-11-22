// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Abstract
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="ITemplateModel.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract
{
    /// <summary>Interface ITemplateModel</summary>
    public interface ITemplateModel : IManifestProducer
    {
        /// <summary>The name of the resource type.</summary>
        string ResourceTypeName { get; }

        /// <summary>A path to the Template that is relative to the root of the template search directory.</summary>
        /// <value>
        ///     <para>(A template in the Template Root directory)</para>
        ///     <blockquote style="margin-right: 0px;" dir="ltr">
        ///         <para style="margin-right: 0px;" dir="ltr">ServiceModel</para>
        ///     </blockquote>
        ///     <para>(a template in a sub directory of the Templates root directory)</para>
        ///     <blockquote style="margin-right: 0px;" dir="ltr">
        ///         <para>Parameters/KeyVault</para>
        ///     </blockquote>
        /// </value>
        /// <remarks>The template path MUST include the template file name (without the ".liquid" file extension).</remarks>
        string TemplatePath { get; }

        /// <summary>The file extension for the files this resource generates.</summary>
        /// <value>json</value>
        string OutputExtension { get; }

        /// <summary>The name of the resource the template is generating.</summary>
        string ResourceName { get; }


        /// <summary>FOR INTERNAL USE ONLY</summary>
        Dictionary<string, object> ToTemplateContext();

        /// <summary>FOR INTERNAL USE ONLY</summary>
        /// <param name="basePath">The base output path.</param>
        string ProduceOutputPath(string basePath);
    }
}