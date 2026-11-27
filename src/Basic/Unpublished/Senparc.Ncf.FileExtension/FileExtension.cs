/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileExtension.cs
    File description: FileExtension related implementation

    Created by: Senparc - 20211124

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.0.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Ncf.FileExtension
{
    /// <summary>
    /// Provides simple asynchronous upload helpers that copy an
    /// <see cref="IFormFile"/> into a server-side file path.
    /// </summary>
    public static class FileExtension
    {
        /// <summary>
        /// Writes an uploaded form file to the exact output path, creating its
        /// parent directory when the implementation can resolve one.
        /// </summary>
        /// <param name="formFile">The uploaded file to copy.</param>
        /// <param name="outPath">The complete destination path, including the file name.</param>
        /// <returns>
        /// A task that completes with <see langword="true"/> after the destination
        /// stream has been flushed.
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when <paramref name="formFile"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="IOException">
        /// Propagated when the destination directory or file cannot be created or written.
        /// </exception>
        public static async Task<bool> Upload(IFormFile formFile, string outPath)
        {
            if (formFile == null || formFile.Length <= 0)
            {
                throw new NullReferenceException("IFormFile is null");
            }
            // full path to file in temp location
            var filePath = Path.GetDirectoryName(outPath);
            if (!System.IO.File.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            using (var stream = new FileStream(outPath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
                stream.Flush();
            }
            return true;
        }

        /// <summary>
        /// Combines a destination directory and file name, then writes the uploaded
        /// form file to the resulting path.
        /// </summary>
        /// <param name="formFile">The uploaded file to copy.</param>
        /// <param name="fileName">The file name to create below <paramref name="paths"/>.</param>
        /// <param name="paths">The destination directory.</param>
        /// <returns>
        /// A task that completes with <see langword="true"/> after the destination
        /// stream has been flushed.
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when <paramref name="formFile"/> is <see langword="null"/> or empty.
        /// </exception>
        /// <exception cref="IOException">
        /// Propagated when the destination directory or file cannot be created or written.
        /// </exception>
        public static async Task<bool> Upload(IFormFile formFile, string fileName, string paths)
        {
            if (formFile == null || formFile.Length <= 0)
            {
                throw new NullReferenceException("IFormFile is null");
            }
            // full path to file in temp location
            if (!System.IO.File.Exists(paths))
            {
                Directory.CreateDirectory(paths);
            }
            var filePath = Path.Combine(paths, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
                stream.Flush();
            }
            return true;
        }
    }
}
