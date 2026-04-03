using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileManager.Domain.Models.DatabaseModel
{
    //[Table(Register.DATABASE_PREFIX + nameof(NcfFile))]//A prefix must be added to prevent conflicts in the entire system

    [Table(Register.DATABASE_PREFIX + nameof(NcfFile))]//The prefix must be added to prevent conflicts system-wide.
    public class NcfFile : EntityBase<int>
    {
        [Required]
        [MaxLength(250)]
        public string FileName { get; set; }

        [Required]
        public string StorageFileName { get; set; } // File name in Guid format

        [Required]
        public string FilePath { get; set; } // Relative path, for example: App_Data/NcfFiles/yyyy/MM/

        public long FileSize { get; set; } // File size (bytes)

        [MaxLength(100)]
        public string FileExtension { get; set; } // file extension

        public FileType FileType { get; set; } // file type enum

        [MaxLength(300)]
        public string Description { get; set; } // File description

        public DateTime UploadTime { get; set; } // Upload time

        public int? FolderId { get; set; } // The folder to which it belongs. It can be empty to indicate the root directory.
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

}