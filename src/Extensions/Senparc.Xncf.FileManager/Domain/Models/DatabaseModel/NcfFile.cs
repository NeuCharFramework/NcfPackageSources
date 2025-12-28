using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileManager.Domain.Models.DatabaseModel
{
    //[Table(Register.DATABASE_PREFIX + nameof(NcfFile))]//必须添加前缀，防止全系统中发生冲突

    [Table(name: "NcfFiles")]
    public class NcfFile : EntityBase<int>
    {
        [Required]
        [MaxLength(250)]
        public string FileName { get; set; }

        [Required]
        public string StorageFileName { get; set; } // Guid 格式的文件名

        [Required]
        public string FilePath { get; set; } // 相对路径，例如：App_Data/NcfFiles/yyyy/MM/

        public long FileSize { get; set; } // 文件大小（字节）

        [MaxLength(100)]
        public string FileExtension { get; set; } // 文件扩展名

        public FileType FileType { get; set; } // 文件类型枚举

        [MaxLength(300)]
        public string Description { get; set; } // 文件描述

        public DateTime UploadTime { get; set; } // 上传时间

        public int? FolderId { get; set; } // 所属文件夹，可为空表示根目录
    }

    public enum FileType
    {
        Text = 0,
        Word = 1,
        PowerPoint = 2,
        Excel = 3,
        Code = 4,
        Other = 999
    }

    // 新增文件夹实体
    [Table(name: "NcfFolders")]
    public class NcfFolder : EntityBase<int>
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public int? ParentId { get; set; } // 父级文件夹，null 为根

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    }
}