﻿// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Writer
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/20/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/20/2023 2:33 PM
// // ***********************************************************************
// // <copyright file="Extensions.cs" company="Joshua S. Irwin">
// //     Copyright (c) 2026 Joshua S. Irwin. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

using UTMO.Text.FileGenerator.Abstract.Constants;

namespace UTMO.Text.FileGenerator.DefaultFileWriter;

using Serilog;

public static class Extensions
{
    /// <summary>
    /// Normalizes a file path between Windows and Linux.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path string.</returns>
    public static string NormalizePath(this string path)
    {
        string pathValue;

        try
        {
            pathValue = Path.GetFullPath(Path.IsPathRooted(path) ? new Uri(path, UriKind.Absolute).LocalPath : path)
                            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Error normalizing path \"{Path}\"", path);
            Environment.Exit(ExitCodes.PathNormalizationError);
            throw;
        }

        pathValue = pathValue.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);

        return pathValue;
    }
}