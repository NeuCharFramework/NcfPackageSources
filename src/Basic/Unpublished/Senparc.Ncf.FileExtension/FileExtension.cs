/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileExtension.cs
    文件功能描述：FileExtension 相关实现
    
    
    创建标识：Senparc - 20211124
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        /// <param name="outPath">绝对路径</param>
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
