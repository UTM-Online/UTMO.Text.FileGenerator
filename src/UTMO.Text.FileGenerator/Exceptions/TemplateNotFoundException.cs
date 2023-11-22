// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="TemplateNotFoundException.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Exceptions
{
    /// <summary>
    ///     occurs when a template is not found.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class TemplateNotFoundException : ApplicationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TemplateNotFoundException" /> class.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="templateSearchPath">The template search path.</param>
        public TemplateNotFoundException(string templateName, string templateSearchPath)
            : base($"Template {templateName} not found when looking in {templateSearchPath}.")
        {
            this.TemplateName = templateName;
            this.TemplateSearchPath = templateSearchPath;
        }

        /// <summary>
        ///     Gets the name of the template.
        /// </summary>
        /// <value>The name of the template.</value>
        public string TemplateName { get; private set; }

        /// <summary>
        ///     Gets the template search path.
        /// </summary>
        /// <value>The template search path.</value>
        public string TemplateSearchPath { get; private set; }
    }
}