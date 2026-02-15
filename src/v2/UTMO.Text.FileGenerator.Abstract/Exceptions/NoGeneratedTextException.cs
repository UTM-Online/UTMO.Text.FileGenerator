﻿// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="NoGeneratedTextException.cs" company="Joshua S. Irwin">
//     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Exceptions
{
    /// <summary>
    ///     Occurs when no text was generated for a template.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class NoGeneratedTextException : ApplicationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NoGeneratedTextException" /> class.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="outputFileName">Name of the output file.</param>
        public NoGeneratedTextException(string templateName, string outputFileName)
            : base($"No text was generated for template {templateName} to output file {outputFileName}")
        {
        }
    }
}