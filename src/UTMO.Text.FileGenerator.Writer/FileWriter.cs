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
    using Abstract;
    using Exceptions;

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
    }
}