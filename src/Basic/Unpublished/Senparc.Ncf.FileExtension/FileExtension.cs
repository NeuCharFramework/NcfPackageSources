/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileExtension.cs
    File description: FileExtension related implementation

    Created by: Senparc - 20211124

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Ncf.FileExtension
{
    public static class FileExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="formFile"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="fileName"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
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
