// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-12-2023
// ***********************************************************************
// <copyright file="MemberNameAttribute.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Attributes
{
    /// <summary>
    ///     When applied to a property or field it instructs the context builder to use the value in the attributes name
    ///     property instead of the name of the decorated member.
    /// </summary>
    /// <remarks>
    ///     This attribute is only required when the name of the decorated field or property is different then the name
    ///     used in the template being generated.
    /// </remarks>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class MemberNameAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MemberNameAttribute" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public MemberNameAttribute(string name)
        {
            this.Name = name;
        }

        /// <summary>
        ///     Gets the name to refer to the members value.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }
    }
}