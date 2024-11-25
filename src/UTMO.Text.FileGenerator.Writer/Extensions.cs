// // ***********************************************************************
// // Assembly         : MD.MIF.FileGenerator.Writer
// // Author           : Josh Irwin (joirwi)
// // Created          : 11/20/2023
// //
// // Last Modified By : Josh Irwin (joirwi)
// // Last Modified On : 11/20/2023 2:33 PM
// // ***********************************************************************
// // <copyright file="Extensions.cs" company="Microsoft Corp">
// //     Copyright (c) Microsoft Corporation. All rights reserved.
// // </copyright>
// // <summary></summary>
// // ***********************************************************************

namespace UTMO.Text.FileGenerator.Writer
{
    using UTMO.Text.FileGenerator.Abstract;

    public static class Extensions
    {
        /// <summary>
        /// Normalizes a file path between Windows and Linux.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path string.</returns>
        public static string NormalizePath(this string path)
        {
            var pathValue = Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            
            if(Path.DirectorySeparatorChar == '/')
            {
                pathValue = pathValue.Replace('\\', Path.DirectorySeparatorChar);
            }
            else
            {
                pathValue = pathValue.Replace('/', Path.DirectorySeparatorChar);
            }
            
            return pathValue;
        }

        public static void RegisterFileWriter(this IRegisterPluginManager rpm)
        {
            rpm.RegisterDependency<IGeneralFileWriter, FileWriter>();
        }

        public static void RegisterFileWriter(this IPluginManager pm)
        {
            pm.RegisterDependency<IGeneralFileWriter, FileWriter>();
        }
    }
}