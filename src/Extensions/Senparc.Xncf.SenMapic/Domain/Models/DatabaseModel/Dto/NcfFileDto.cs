using System;

namespace Senparc.Xncf.SenMapic.Domain.Models.DatabaseModel.Dto
{
    public class NcfFileDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public long FileSize { get; set; }
        public FileType FileType { get; set; }
        public string Description { get; set; }
        public DateTime UploadTime { get; set; }
        
        // 用于前端显示的属性
        public string FileSizeDisplay => GetFileSizeDisplay();
        public string FileTypeDisplay => FileType.ToString();

        private string GetFileSizeDisplay()
        {
            if (FileSize < 1024) return $"{FileSize}B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F2}KB";
            return $"{FileSize / (1024.0 * 1024):F2}MB";
        }
    }
} 