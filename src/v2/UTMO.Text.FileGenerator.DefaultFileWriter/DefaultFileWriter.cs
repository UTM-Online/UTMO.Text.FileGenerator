// // ***********************************************************************
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
using System.Text;
using Abstract;
using UTMO.Text.FileGenerator.Abstract.Contracts;
using UTMO.Text.FileGenerator.DefaultFileWriter.Exceptions;

// ReSharper disable once ClassNeverInstantiated.Global
public class DefaultFileWriter : IGeneralFileWriter
{
    private static readonly string[] LinuxSystemPathPrefixes =
    {
        "/etc/",
        "/sys/",
        "/proc/",
        "/root/",
        "/var/",
        "/boot/",
        "/dev/",
        "/usr/bin/",
        "/usr/sbin/",
        "/sbin/",
        "/bin/"
    };

    private static readonly string[] WindowsSystemPathPrefixes = BuildWindowsSystemPathPrefixes();

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

        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(path)
                .Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            throw new InvalidOutputDirectoryException();
        }
        catch (NotSupportedException)
        {
            throw new InvalidOutputDirectoryException();
        }
        catch (PathTooLongException)
        {
            throw new InvalidOutputDirectoryException();
        }

        // Handle Windows extended-length path prefixes such as \\?\c:\windows\...
        if (normalizedPath.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath[4..];
        }

        normalizedPath = normalizedPath.Replace('\\', '/');
        var normalizedPathWithTrailingSeparator = normalizedPath.TrimEnd('/') + "/";
        var blockedPrefixes = OperatingSystem.IsWindows() ? WindowsSystemPathPrefixes : LinuxSystemPathPrefixes;

        if (blockedPrefixes.Any(prefix => normalizedPathWithTrailingSeparator.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOutputDirectoryException();
        }
    }

    private static string[] BuildWindowsSystemPathPrefixes()
    {
        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddSystemPathPrefix(prefixes, GetWindowsDirectory());
        AddSystemPathPrefix(prefixes, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddSystemPathPrefix(prefixes, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        AddSystemPathPrefix(prefixes, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

        var userProfileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var usersDirectory = string.IsNullOrWhiteSpace(userProfileDirectory)
            ? null
            : Directory.GetParent(userProfileDirectory)?.FullName;

        if (!string.IsNullOrWhiteSpace(usersDirectory))
        {
            AddSystemPathPrefix(prefixes, Path.Join(usersDirectory, "Default"));
            AddSystemPathPrefix(prefixes, Path.Join(usersDirectory, "Public"));
            AddSystemPathPrefix(prefixes, Path.Join(usersDirectory, "Administrator"));
        }

        return prefixes.ToArray();
    }

    private static string GetWindowsDirectory()
    {
        var systemDirectory = Environment.SystemDirectory;
        if (!string.IsNullOrWhiteSpace(systemDirectory))
        {
            var windowsDirectory = Directory.GetParent(systemDirectory)?.FullName;
            if (!string.IsNullOrWhiteSpace(windowsDirectory))
            {
                return windowsDirectory;
            }
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    }

    private static void AddSystemPathPrefix(ISet<string> prefixes, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalizedPath = path.NormalizePath()
            .Replace('\\', '/')
            .TrimEnd('/', '\\') + "/";
        prefixes.Add(normalizedPath);
    }
}
