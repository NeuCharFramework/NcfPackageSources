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

namespace Senparc.Xncf.FileManager.Domain.Services
{
    public class NcfFileService : ServiceBase<NcfFile>
    {
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
            var datePath = Path.Combine(DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString("00"));
            var fullPath = Path.Combine(_baseFilePath, datePath);
            Directory.CreateDirectory(fullPath);

            var storageFileName = Guid.NewGuid().ToString("N");
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            var physicalPath = Path.Combine(fullPath, storageFileName + fileExtension);
            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var ncfFile = new NcfFile
            {
                FileName = file.FileName,
                StorageFileName = storageFileName,
                FilePath = datePath,
                FileSize = file.Length,
                FileExtension = fileExtension,
                FileType = GetFileType(fileExtension),
                UploadTime = DateTime.Now,
                FolderId = folderId
            };

            await SaveObjectAsync(ncfFile);
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
                var fullPath = Path.Combine(_baseFilePath, file.FilePath, file.StorageFileName + file.FileExtension);
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
            var fullPath = Path.Combine(_baseFilePath, file.FilePath, fileName);
            if (!System.IO.File.Exists(fullPath))
            {
                return (new byte[0], "文件不存在！");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return (bytes, fileName);
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