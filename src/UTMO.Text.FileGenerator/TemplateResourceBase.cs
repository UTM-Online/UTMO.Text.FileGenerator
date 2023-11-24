// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-20-2023
// ***********************************************************************
// <copyright file="TemplateResourceBase.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

#pragma warning disable VSSpell001
namespace UTMO.Text.FileGenerator
#pragma warning restore VSSpell001
{
    using System.Reflection;
    using Abstract;
    using Attributes;

    /// <summary>
    ///     The base class for resources that are used to supply values to the rendering context.
    ///     Implements the <see cref="ITemplateModel" />
    /// </summary>
    /// <seealso cref="ITemplateModel" />
    public abstract class TemplateResourceBase : ITemplateModel, IManifestProducer
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        /// <summary>
        ///     The additional properties
        /// </summary>
        protected Dictionary<string, object> AdditionalProperties = new();

        /// <summary>
        ///     Gets or sets a value indicating whether [use alternate name].
        /// </summary>
        /// <value><c>true</c> if [use alternate name]; otherwise, <c>false</c>.</value>
        [IgnoreMember]
        public virtual bool UseAlternateName { get; set; } = false;

        /// <summary>
        ///     The name of the resource type.
        /// </summary>
        /// <value>The name of the resource type.</value>
        [IgnoreMember]
        public abstract string ResourceTypeName { get; }

        /// <summary>
        ///     A path to the Template that is relative to the root of the template search directory.
        /// </summary>
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
        [IgnoreMember]
        public abstract string TemplatePath { get; }

        /// <summary>
        ///     The file extension for the files this resource generates.
        /// </summary>
        /// <example>json</example>
        [IgnoreMember]
        public abstract string OutputExtension { get; }

        /// <summary>
        ///     The name of the resource the template is generating.
        /// </summary>
        /// <value>The name of the resource.</value>
        [IgnoreMember]
        public abstract string ResourceName { get; }

        /// <summary>
        ///     FOR INTERNAL USE ONLY
        /// </summary>
        /// <returns>Dictionary&lt;System.String, System.Object&gt;.</returns>
        /// <exception cref="AmbiguousMatchException">More than one of the requested attributes was found.</exception>
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded.</exception>
        public virtual Dictionary<string, object> ToTemplateContext()
        {
            var properties = new Dictionary<string, object>();

            var propertyBag = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => !x.GetCustomAttributes<IgnoreMemberAttribute>(true).Any());

            foreach (var prop in propertyBag)
            {
                var propertyName = prop.GetCustomAttribute<MemberNameAttribute>(true)?.Name ?? prop.Name;
                var propertyValue = prop.GetValue(this);

                switch (propertyValue)
                {
                    case null:
                        continue;

                    case TemplateResourceBase templateResource:
                        properties.Add(propertyName, templateResource.ToTemplateContext());
                        break;

                    case IEnumerable<TemplateResourceBase> resources:
                    {
                        var nestedProp = resources.Select(resource => resource.ToTemplateContext()).ToList();

                        properties.Add(propertyName, nestedProp);
                        break;
                    }

                    default:
                        properties.Add(propertyName, propertyValue);
                        break;
                }
            }

            // ReSharper disable once InvertIf
            if (this.AdditionalProperties.Any())
            {
                foreach (var prop in this.AdditionalProperties)
                {
                    properties.Add(prop.Key, prop.Value);
                }
            }

            return properties;
        }

        /// <summary>
        ///     FOR INTERNAL USE ONLY
        /// </summary>
        /// <param name="basePath">The base output path.</param>
        /// <returns>System.String.</returns>
        public virtual string ProduceOutputPath(string basePath)
        {
            return Path.Join(basePath, this.ResourceTypeName,
                this.UseAlternateName ? $"{this.ResourceName}.{this.ResourceTypeName}.{this.OutputExtension.TrimStart('.')}" : $"{this.ResourceName}.{this.OutputExtension.TrimStart('.')}");
        }

        /// <summary>
        ///     Adds the additional property.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>The resource instance.</returns>
        /// <exception cref="System.ArgumentNullException">The <paramref name="value" /> was evaluated as null.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the local context.</exception>
        protected virtual TemplateResourceBase AddAdditionalProperty<T>(string key, T value)
        {
            this.AdditionalProperties.Add(key, value ?? throw new ArgumentNullException(nameof(value)));
            return this;
        }

        /// <summary>
        /// Indicates if the current resource should generate a manifest entry.
        /// </summary>
        public abstract bool GenerateManifest { get; }
        
        /// <summary>
        /// Generates the resource manifest.
        /// </summary>
        /// <returns>The resources manifest.</returns>
        public virtual dynamic? ToManifest()
        {
            return null;
        }
    }
}