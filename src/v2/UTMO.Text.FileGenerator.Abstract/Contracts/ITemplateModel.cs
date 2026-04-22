﻿// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Abstract
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="ITemplateModel.cs" company="Joshua S. Irwin">
//     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Contracts
{
    using UTMO.Text.FileGenerator.Abstract.Exceptions;

    /// <summary>Interface ITemplateModel</summary>
    public interface ITemplateModel
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
        
        bool EnableGeneration { get; }
        
        
        bool UseAlternateName { get; }
        
        Task<List<ValidationFailedException>> Validate();

        /// <summary>
        /// FOR INTERNAL USE ONLY - Builds the template rendering context from template-facing properties and any
        /// additional properties added programmatically.
        /// </summary>
        /// <remarks>
        ///     When the <c>LegacyNonPublicTemplateProperties</c> feature flag is disabled (the secure default), only
        ///     public properties explicitly decorated with
        ///     <see cref="UTMO.Text.FileGenerator.Attributes.TemplatePropertyAttribute"/> are included in the returned
        ///     dictionary. Use <c>[IgnoreMember]</c> to explicitly exclude a property that is decorated with
        ///     <c>[TemplateProperty]</c>. Properties added via <c>AddAdditionalProperty</c> are always included
        ///     regardless of attributes.
        ///     <para>
        ///         <b>Legacy compatibility:</b> When the <c>LegacyNonPublicTemplateProperties</c> feature flag is
        ///         enabled, the returned dictionary may also include non-public properties (without
        ///         <c>[TemplateProperty]</c>) for backward compatibility. <b>This is a security risk</b> as it can
        ///         expose members that are not part of the normal explicit template surface. This legacy mode should
        ///         not be relied on for new development.
        ///     </para>
        ///     <para>
        ///         <b>Migration:</b> Enable the <c>LegacyNonPublicTemplateProperties</c> feature flag only as a
        ///         temporary aid when migrating from older versions. When enabled, legacy behavior may expose
        ///         non-public properties to templates, which is a security risk, and public template-facing properties
        ///         must still be decorated with <c>[TemplateProperty]</c>. This mode is deprecated and should be
        ///         disabled as soon as migration is complete and all intended public template-facing properties have
        ///         been explicitly decorated with <c>[TemplateProperty]</c>.
        ///     </para>
        /// </remarks>
        Task<Dictionary<string, object>> ToTemplateContext();

        /// <summary>FOR INTERNAL USE ONLY</summary>
        /// <param name="basePath">The base output path.</param>
        string ProduceOutputPath(string basePath);
        
        ITemplateModel AddAdditionalProperty<T>(string key, T value);
    }
}