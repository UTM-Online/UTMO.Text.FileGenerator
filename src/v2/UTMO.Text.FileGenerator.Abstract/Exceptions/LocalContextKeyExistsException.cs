// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="LocalContextKeyExistsException.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Exceptions
{
    /// <summary>
    ///     Occurs when a key already exists in the local rendering context.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class LocalContextKeyExistsException : ApplicationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LocalContextKeyExistsException" /> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public LocalContextKeyExistsException(string key) : base($"The local context already contains a key with the name {key}")
        {
            this.Key = key;
        }

        /// <summary>
        ///     Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; private set; }
    }
}