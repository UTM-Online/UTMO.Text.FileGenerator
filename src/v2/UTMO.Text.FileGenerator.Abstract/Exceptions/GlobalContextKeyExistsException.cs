// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="GlobalContextKeyExistsException.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Exceptions
{
    /// <summary>
    ///     The supplied key already exists in the global context.
    /// </summary>
    /// <seealso cref="System.ApplicationException" />
    public class GlobalContextKeyExistsException : ApplicationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalContextKeyExistsException" /> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public GlobalContextKeyExistsException(string key) : base($"The specified key {key} already exists in the global context.")
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