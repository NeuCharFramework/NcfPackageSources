/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfFileDto.cs
    File description: NcfFileDto related implementation

    Created by: Senparc - 20250112

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

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
        public NcfFileResourceScope ResourceScope { get; set; }
        public NcfFileAccessLevel AccessLevel { get; set; }
        public string ContentType { get; set; }
        public string ContentHash { get; set; }
        public string PublicUrl { get; set; }
        
        //Legacy comment translated to English.
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
