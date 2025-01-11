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
        private readonly string _baseFilePath;
        public IMapper Mapper { get; set; }

        public NcfFileService(IRepositoryBase<NcfFile> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
            _baseFilePath = Path.Combine(AppContext.BaseDirectory, "App_Data", "NcfFiles");
            InitMapper();
        }

        private void InitMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<NcfFile, NcfFileDto>();
                cfg.CreateMap<NcfFileDto, NcfFile>();
            });

            Mapper = config.CreateMapper();
        }

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

        public async Task<byte[]> GetFileBytes(int id)
        {
            var file = await GetObjectAsync(z => z.Id == id);
            if (file == null) return null;

            var fullPath = Path.Combine(_baseFilePath, file.FilePath, file.StorageFileName + file.FileExtension);
            return await File.ReadAllBytesAsync(fullPath);
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