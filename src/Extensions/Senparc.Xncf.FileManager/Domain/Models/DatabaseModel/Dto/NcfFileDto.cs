/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfFileDto.cs
    文件功能描述：NcfFileDto 相关实现
    
    
    创建标识：Senparc - 20250112
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto
{
    public class NcfFileDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
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