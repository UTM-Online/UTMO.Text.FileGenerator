// ***********************************************************************
// Assembly         : UTMO.Text.FileGenerator.Abstract
// Author           : Josh Irwin (joirwi)
// Created          : 03-03-2026
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 03-03-2026
// ***********************************************************************
// <copyright file="TemplatePropertyAttribute.cs" company="Joshua S. Irwin">
//     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Attributes
{
    /// <summary>
    ///     When applied to a property, it explicitly marks the property as safe to expose to template contexts.
    ///     This is an opt-in security feature to prevent accidental exposure of sensitive data.
    ///     Only properties decorated with this attribute will be included in the template rendering context.
    ///     Implements the <see cref="System.Attribute" />
    /// </summary>
    /// <remarks>
    ///     This attribute is required for any property that should be accessible in templates.
    ///     Properties without this attribute will not be exposed to templates, providing a secure-by-default approach.
    /// </remarks>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class TemplatePropertyAttribute : Attribute
    {
    }
}

