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

namespace Senparc.Xncf.FileManager.Domain.Services
{
    public class NcfFileService : ServiceBase<NcfFile>
    {
        /// <summary>
        /// 文件存储的基础路径
        /// </summary>
        private readonly string _baseFilePath;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="repo">文件仓储接口</param>
        /// <param name="serviceProvider">服务提供者</param>
        public NcfFileService(IRepositoryBase<NcfFile> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
            _baseFilePath = Path.Combine(AppContext.BaseDirectory, "App_Data", "NcfFiles");
           
           // 尝试添加目录
           Senparc.CO2NET.Helpers.FileHelper.TryCreateDirectory(_baseFilePath);
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <param name="description">文件描述</param>
        /// <returns>文件信息DTO</returns>
        public async Task<NcfFileDto> UploadFile(IFormFile file, string description = null)
        {
            // 确保目录存在
            var datePath = Path.Combine(DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString("00"));
            var fullPath = Path.Combine(_baseFilePath, datePath);
            Directory.CreateDirectory(fullPath);

            // 生成存储文件名
            var storageFileName = Guid.NewGuid().ToString("N");
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            // 保存文件
            var physicalPath = Path.Combine(fullPath, storageFileName + fileExtension);
            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 创建数据库记录
            var ncfFile = new NcfFile
            {
                FileName = file.FileName,
                StorageFileName = storageFileName,
                FilePath = Path.Combine(datePath),
                FileSize = file.Length,
                FileExtension = fileExtension,
                FileType = GetFileType(fileExtension),
                Description = description,
                UploadTime = DateTime.Now
            };

            await base.SaveObjectAsync(ncfFile).ConfigureAwait(false);

            return Mapper.Map<NcfFileDto>(ncfFile);
        }

        /// <summary>
        /// 获取文件字节数组
        /// </summary>
        /// <param name="id">文件ID</param>
        /// <returns>文件字节数组</returns>
        public async Task<byte[]> GetFileBytes(int id)
        {
            var file = await GetObjectAsync(z => z.Id == id);
            if (file == null) return null;

            var fullPath = Path.Combine(_baseFilePath, file.FilePath, file.StorageFileName + file.FileExtension);
            return await File.ReadAllBytesAsync(fullPath);
        }

        /// <summary>
        /// 根据文件扩展名获取文件类型
        /// </summary>
        /// <param name="extension">文件扩展名</param>
        /// <returns>文件类型枚举</returns>
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

        /// <summary>
        /// 异步上传文件
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <returns>文件实体</returns>
        public async Task<NcfFile> UploadFileAsync(IFormFile file)
        {
            // 确保目录存在
            var datePath = Path.Combine(DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString("00"));
            var fullPath = Path.Combine(_baseFilePath, datePath);
            Directory.CreateDirectory(fullPath);

            // 生成存储文件名
            var storageFileName = Guid.NewGuid().ToString("N");
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            // 保存文件
            var physicalPath = Path.Combine(fullPath, storageFileName + fileExtension);
            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 创建数据库记录
            var ncfFile = new NcfFile
            {
                FileName = file.FileName,
                StorageFileName = storageFileName,
                FilePath = datePath,
                FileSize = file.Length,
                FileExtension = fileExtension,
                FileType = GetFileType(fileExtension),
                UploadTime = DateTime.Now
            };

            await SaveObjectAsync(ncfFile);
            return ncfFile;
        }

        /// <summary>
        /// Updates the note/description of a file
        /// </summary>
        public async Task UpdateFileNoteAsync(int id, string note)
        {
            var file = await GetObjectAsync(z => z.Id == id);
            if (file != null)
            {
                file.Description = note;
                await SaveObjectAsync(file);
            }
        }

        /// <summary>
        /// Deletes a file from storage and database
        /// </summary>
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
    }
} 