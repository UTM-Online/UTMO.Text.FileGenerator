﻿// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Writer
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/20/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/20/2023 2:30 PM
// // ***********************************************************************
// // <copyright file="FileWriter.cs" company="Joshua S. Irwin">
// //     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
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
        // Validate path BEFORE normalization to catch traversal attempts
        ValidateOutputPathBeforeNormalization(fileName);
        
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
        // Validate paths BEFORE normalization
        ValidateOutputPathBeforeNormalization(fileName);
        ValidateOutputPathBeforeNormalization(outputPath);
        
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

    /// <summary>
    /// Validates that the output path doesn't contain suspicious patterns before normalization.
    /// This catches path traversal attempts before Path.GetFullPath() resolves them.
    /// </summary>
    /// <param name="path">The path to validate before normalization.</param>
    /// <exception cref="InvalidOutputDirectoryException">Thrown when path contains suspicious patterns.</exception>
    private static void ValidateOutputPathBeforeNormalization(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOutputDirectoryException();
        }

        // Check for path traversal patterns before normalization
        if (path.Contains("..") || path.Contains("~"))
        {
            throw new InvalidOutputDirectoryException();
        }

        // Additional validation: ensure path doesn't try to access system directories
        var lowerPath = path.ToLowerInvariant().Replace('\\', '/');
        
        // Block access to system directories - only block root-level system paths, not user directories
        var systemPaths = new[] 
        { 
            "/etc/", "/sys/", "/proc/", "/root/", "/var/", "/boot/",
            "c:/windows/", "c:/program files/", "c:/program files (x86)/",
            "c:/programdata/"
        };
        
        if (systemPaths.Any(pattern => lowerPath.Contains(pattern)))
        {
            throw new InvalidOutputDirectoryException();
        }
    }
}