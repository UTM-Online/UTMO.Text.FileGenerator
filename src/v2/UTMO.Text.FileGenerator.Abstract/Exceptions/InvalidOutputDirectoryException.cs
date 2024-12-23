// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="InvalidOutputDirectoryException.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Exceptions
{
    /// <summary>
    ///     The output file specified resulted in an invalid output directory being calculated.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class InvalidOutputDirectoryException : ApplicationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InvalidOutputDirectoryException" /> class.
        /// </summary>
        public InvalidOutputDirectoryException()
            : base("The output file specified resulted in an invalid output directory being calculated.")
        {
        }
    }
}