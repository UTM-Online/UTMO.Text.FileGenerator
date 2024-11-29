// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Writer
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/20/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/20/2023 2:30 PM
// // ***********************************************************************
// // <copyright file="FileWriter.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Writer
{
    using System.Reflection;
    using Exceptions;
    using UTMO.Text.FileGenerator.Abstract;

    public class FileWriter : IGeneralFileWriter
    {
        public void WriteFile(string fileName, string content, bool overwrite = false)
        {
            fileName = fileName.NormalizePath();
            
            var outputDirectory = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(outputDirectory) && !string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            else if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOutputDirectoryException();
            }

            if (!overwrite && File.Exists(fileName))
            {
                throw new ApplicationException($"The file \"{fileName}\" already exists.");
            }

            using var writer = new StreamWriter(File.Create(fileName));
            writer.Write(content);
        }

        public void WriteEmbeddedResource(string fileName, string outputPath, EmbeddedResourceType resourceType)
        {
            fileName = fileName.NormalizePath();
            outputPath = outputPath.NormalizePath();
            
            var outputDirectory = Path.GetDirectoryName(outputPath);

            if (!Directory.Exists(outputDirectory) && !string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            else if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOutputDirectoryException();
            }

            if (File.Exists(outputPath))
            {
                throw new ApplicationException($"The file \"{outputPath}\" already exists.");
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"UTMO.Text.FileGenerator.Resources.{fileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new ApplicationException($"The embedded resource \"{resourceName}\" could not be found.");
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            using var writer = new StreamWriter(File.Create(outputPath));
            writer.Write(content);
        }
    }
}