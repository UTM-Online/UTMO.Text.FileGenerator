// ***********************************************************************
// Assembly         : MD.MIF.FileGenerator.Core
// Author           : Josh Irwin (joirwi)
// Created          : 10-12-2023
//
// Last Modified By : Josh Irwin (joirwi)
// Last Modified On : 10-20-2023
// ***********************************************************************
// <copyright file="TemplateRenderer.cs" company="Microsoft Corp">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace UTMO.Text.FileGenerator
{
    using Abstract;
    using DotLiquid;
    using Exceptions;
    using Extensions;
    using Writer;

    /// <summary>
    ///     Class TemplateRenderer.
    ///     Implements the <see cref="ITemplateRenderer" />
    /// </summary>
    /// <seealso cref="ITemplateRenderer" />
    public class TemplateRenderer : ITemplateRenderer
    {
        /// <summary>
        ///     The global context
        /// </summary>
        private readonly Dictionary<string, object> _globalContext;

        /// <summary>
        ///     The template directory
        /// </summary>
        private readonly string _templateDirectory;
        
        private readonly IGeneralFileWriter _fileWriter;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TemplateRenderer" /> class.
        /// </summary>
        /// <param name="templateDirectory">The template directory.</param>
        /// <param name="writer">The file writer.</param>
        public TemplateRenderer(string templateDirectory, IGeneralFileWriter writer)
        {
            this._templateDirectory = templateDirectory;
            this._globalContext = new Dictionary<string, object>();
            this._fileWriter = writer;
        }

        /// <summary>
        ///     Generates the file.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="outputFileName">Name of the output file.</param>
        /// <param name="dict">The rendering context.</param>
        /// <exception cref="TemplateNotFoundException">templateName, this._templateDirectory</exception>
        /// <exception cref="NoGeneratedTextException">templateName, outputFileName</exception>
        /// <exception cref="InvalidOutputDirectoryException"></exception>
        public void GenerateFile(string templateName, string outputFileName, Dictionary<string, object> dict)
        {
            if (!templateName.EndsWith(".liquid"))
            {
                templateName = string.Concat(templateName, ".liquid");
            }

            if (this._globalContext.Any())
            {
                dict.Merge(this._globalContext);
            }

            var templatePath = Path.Combine(this._templateDirectory, templateName).NormalizePath();

            if (!File.Exists(templatePath))
            {
                throw new TemplateNotFoundException(templateName, this._templateDirectory);
            }

            var template = File.ReadAllText(templatePath);
            var parsedTemplate = Template.Parse(template);
            // ReSharper disable once RedundantAssignment
            var result = string.Empty;

            try
            {
                result = parsedTemplate.Render(Hash.FromDictionary(dict));
            }
            catch (Exception ex)
            {
                if (ex is DirectoryNotFoundException or FileNotFoundException)
                {
                    throw new TemplateNotFoundException(templateName, this._templateDirectory);
                }

                throw;
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new NoGeneratedTextException(templateName, outputFileName);
            }

            this._fileWriter.WriteFile(outputFileName, result);
        }

        /// <summary>
        ///     Generates the file.
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="outputFileName">Name of the output file.</param>
        /// <param name="model">The model.</param>
        public void GenerateFile<T>(string templateName, string outputFileName, T model)
            where T : ITemplateModel
        {
            this.GenerateFile(templateName, outputFileName, model.ToTemplateContext());
        }

        /// <summary>
        ///     Adds to the global context.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="GlobalContextKeyExistsException">key</exception>
        public void AddToGlobalContext(string key, object value)
        {
            if (this._globalContext.ContainsKey(key))
            {
                throw new GlobalContextKeyExistsException(key);
            }

            this._globalContext.Add(key, value);
        }
    }
}