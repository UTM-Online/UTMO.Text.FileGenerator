// ***********************************************************************
// Assembly         : UTMO.Text.FileGenerator.Abstract
// Author           : Josh Irwin (joirwi)
// Created          : 04-19-2026
//
// Last Modified By : GitHub Copilot
// Last Modified On : 04-19-2026
// ***********************************************************************
// <copyright file="LegacyTemplatePropertyAttribute.cs" company="Joshua S. Irwin">
//     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator.Abstract.Attributes
{
    using System.ComponentModel;

    /// <summary>
    ///     Backward-compatible alias for <see cref="UTMO.Text.FileGenerator.Attributes.TemplatePropertyAttribute" />.
    ///     Prefer the <c>UTMO.Text.FileGenerator.Attributes</c> namespace for new code so attribute APIs remain consistent.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use UTMO.Text.FileGenerator.Attributes.TemplatePropertyAttribute instead.", false)]
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class LegacyTemplatePropertyAttribute : UTMO.Text.FileGenerator.Attributes.TemplatePropertyAttribute
    {
    }
}

