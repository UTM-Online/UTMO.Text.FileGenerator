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

namespace UTMO.Text.FileGenerator.DefaultFileWriter;

using System.Reflection;
using Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

// ReSharper disable once ClassNeverInstantiated.Global
public class DefaultFileWriter : IGeneralFileWriter
{
    public async Task WriteFile(string fileName, string content, bool overwrite = false)
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

        await using var writer = new StreamWriter(File.Create(fileName));
        await writer.WriteAsync(content);
    }

    public async Task WriteEmbeddedResource(string fileName, string outputPath, EmbeddedResourceType resourceType, Type resourceTypeObject)
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

        var assembly = Assembly.GetAssembly(resourceTypeObject);
        
        if (assembly == null)
        {
            throw new ApplicationException("The assembly could not be found.");
        }
        
        var resourceName = $"{assembly.GetName().Name}.Resources.{fileName}";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new ApplicationException($"The embedded resource \"{resourceName}\" could not be found.");
        }

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        await using var writer = new StreamWriter(File.Create(outputPath));
        await writer.WriteAsync(content);
    }
}