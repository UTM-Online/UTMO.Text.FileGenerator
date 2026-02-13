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
        
        // Validate that the normalized path is safe and doesn't attempt path traversal
        ValidateOutputPath(fileName);
            
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
        
        // Validate that the normalized path is safe and doesn't attempt path traversal
        ValidateOutputPath(outputPath);
            
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
    /// Validates that the output path doesn't attempt to traverse outside allowed directories.
    /// </summary>
    /// <param name="path">The normalized path to validate.</param>
    /// <exception cref="InvalidOutputDirectoryException">Thrown when path contains suspicious patterns.</exception>
    private static void ValidateOutputPath(string path)
    {
        // Check for common path traversal patterns
        if (path.Contains("..") || path.Contains("~"))
        {
            throw new InvalidOutputDirectoryException();
        }

        // Additional validation: ensure path doesn't try to access system directories
        var normalizedLower = path.ToLowerInvariant();
        var suspiciousPatterns = new[] { "/etc/", "/sys/", "/proc/", "/root/", "c:\\windows\\", "c:\\program files\\" };
        
        if (suspiciousPatterns.Any(pattern => normalizedLower.Contains(pattern)))
        {
            throw new InvalidOutputDirectoryException();
        }
    }
}