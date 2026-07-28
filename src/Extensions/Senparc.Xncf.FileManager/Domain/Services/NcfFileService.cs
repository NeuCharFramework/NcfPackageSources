/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfFileService.cs
    文件功能描述：NcfFileService 相关实现
    
    
    创建标识：Senparc - 20250112
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260729
    修改描述：v0.3.1-preview3 加强文件上传校验和物理路径安全

----------------------------------------------------------------*/

using AutoMapper;
using Microsoft.AspNetCore.Http;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Senparc.Ncf.Core.Models;
using Senparc.CO2NET.Trace;

namespace Senparc.Xncf.FileManager.Domain.Services
{
    public class NcfFileService : ServiceBase<NcfFile>
    {
        public const long MaxFileSizeBytes = 50L * 1024 * 1024;
        public const long MaxTotalUploadBytes = 100L * 1024 * 1024;
        public const int MaxFilesPerUpload = 20;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".log", ".md", ".csv", ".json", ".xml", ".pdf",
            ".jpg", ".jpeg", ".png", ".gif", ".webp",
            ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip"
        };

        /// <summary>
        /// 文件存储的基础路径
        /// </summary>
        private readonly string _baseFilePath;

        public NcfFileService(IRepositoryBase<NcfFile> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
            try
            {
                _baseFilePath = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, "App_Data", "NcfFiles");
                Senparc.CO2NET.Helpers.FileHelper.TryCreateDirectory(_baseFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        // 列表（支持按文件夹过滤）
        public async Task<PagedList<NcfFileDto>> GetFilesAsync(int page, int pageSize, int? folderId)
        {
            var result = (await GetObjectListAsync(page, pageSize, z => z.FolderId == folderId, z => z.Id, OrderingType.Descending, null))
                .ToDtoPagedList<NcfFile, NcfFileDto>(this);
            return result;
        }

        public async Task<NcfFile> UploadFileAsync(IFormFile file, int? folderId = null)
        {
            if (file == null || file.Length <= 0)
            {
                throw new ArgumentException("上传文件不能为空。", nameof(file));
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"单个文件不能超过 {MaxFileSizeBytes / 1024 / 1024} MB。");
            }

            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fileExtension) || !AllowedExtensions.Contains(fileExtension))
            {
                throw new InvalidOperationException("不允许上传该文件类型。仅支持常见文档、图片、文本和压缩包格式。");
            }

            var originalFileName = Path.GetFileName((file.FileName ?? string.Empty).Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                originalFileName = $"upload{fileExtension}";
            }
            if (originalFileName.Length > 250)
            {
                originalFileName = originalFileName[..250];
            }

            var datePath = Path.Combine(DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString("00"));
            var fullPath = Path.Combine(_baseFilePath, datePath);
            Directory.CreateDirectory(fullPath);

            var storageFileName = Guid.NewGuid().ToString("N");

            var physicalPath = Path.Combine(fullPath, storageFileName + fileExtension);
            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var ncfFile = new NcfFile
            {
                FileName = originalFileName,
                StorageFileName = storageFileName,
                FilePath = datePath,
                FileSize = file.Length,
                FileExtension = fileExtension,
                FileType = GetFileType(fileExtension),
                UploadTime = DateTime.Now,
                FolderId = folderId
            };

            try
            {
                await SaveObjectAsync(ncfFile);

            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                throw;
            }
            return ncfFile;
        }

        public async Task UpdateFileNoteAsync(int id, string note)
        {
            var file = await GetObjectAsync(z => z.Id == id);
            if (file != null)
            {
                file.Description = note;
                await SaveObjectAsync(file);
            }
        }

        public async Task DeleteFileAsync(int id)
        {
            var file = await GetObjectAsync(z => z.Id == id);
            if (file != null)
            {
                var fullPath = ResolvePhysicalPath(file);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                await DeleteObjectAsync(file);
            }
        }

        public async Task<(byte[] FileBytes, string FileName)> GetFileBytes(int id)
        {
            var file = await GetObjectAsync(z => z.Id == id);
            if (file == null)
            {
                return (new byte[0], "文件不存在！");
            }

            var fileName = file.StorageFileName + file.FileExtension;
            var fullPath = ResolvePhysicalPath(file);
            if (!System.IO.File.Exists(fullPath))
            {
                return (new byte[0], "文件不存在！");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return (bytes, fileName);
        }

        private string ResolvePhysicalPath(NcfFile file)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.FilePath) ||
                string.IsNullOrWhiteSpace(file.StorageFileName) || string.IsNullOrWhiteSpace(file.FileExtension))
            {
                throw new InvalidOperationException("文件物理路径信息无效。");
            }

            var basePath = Path.GetFullPath(_baseFilePath);
            var fullPath = Path.GetFullPath(Path.Combine(
                basePath,
                file.FilePath,
                file.StorageFileName + file.FileExtension));
            var expectedPrefix = basePath.EndsWith(Path.DirectorySeparatorChar)
                ? basePath
                : basePath + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("文件物理路径越界。");
            }

            return fullPath;
        }

        private FileType GetFileType(string extension)
        {
            return extension.ToLower() switch
            {
                ".txt" or ".log" => FileType.Text,
                ".doc" or ".docx" => FileType.Word,
                ".ppt" or ".pptx" => FileType.PowerPoint,
                ".xls" or ".xlsx" => FileType.Excel,
                ".cs" or ".js" or ".html" or ".css" or ".xml" or ".json" => FileType.Code,
                _ => FileType.Other,
            };
        }
    }
}
