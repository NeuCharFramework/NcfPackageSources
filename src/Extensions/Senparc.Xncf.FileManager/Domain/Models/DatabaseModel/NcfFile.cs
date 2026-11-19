/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfFile.cs
    File description: NcfFile related implementation

    Created by: Senparc - 20250112

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.1 add SourceFileId, SourceType, and SourceUrl columns to PromptRange table for PostgreSQL, SQL Server, and SQLite

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileManager.Domain.Models.DatabaseModel
{
    //[Table(Register.DATABASE_PREFIX + nameof(NcfFile))]//a prefix is required to prevent system-wide conflicts

    [Table(Register.DATABASE_PREFIX + nameof(NcfFile))]//a prefix is required to prevent system-wide conflicts
    public class NcfFile : EntityBase<int>
    {
        [Required]
        [MaxLength(250)]
        public string FileName { get; set; }

        [Required]
        public string StorageFileName { get; set; } //Legacy comment translated to English.

        [Required]
        public string FilePath { get; set; } //Legacy comment translated to English.

        public long FileSize { get; set; } //Legacy comment translated to English.

        [MaxLength(100)]
        public string FileExtension { get; set; } //Legacy comment translated to English.

        public FileType FileType { get; set; } //Legacy comment translated to English.

        [MaxLength(300)]
        public string Description { get; set; } // filedescription

        public DateTime UploadTime { get; set; } //Legacy comment translated to English.

        public int? FolderId { get; set; } //Legacy comment translated to English.

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public NcfFileResourceScope ResourceScope { get; set; } = NcfFileResourceScope.KnowledgeBase;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public NcfFileAccessLevel AccessLevel { get; set; } = NcfFileAccessLevel.Private;

        [MaxLength(200)]
        public string ContentType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [MaxLength(64)]
        public string ContentHash { get; set; }
    }

    public enum NcfFileResourceScope
    {
        ///Legacy comment translated to English.
        KnowledgeBase = 100,

        ///Legacy comment translated to English.
        SiteAsset = 200,

        ///Legacy comment translated to English.
        PrivateAttachment = 300,

        /// <summary>Skill package and resource files.</summary>
        Skill = 400
    }

    public enum NcfFileAccessLevel
    {
        Private = 0,
        Public = 100
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
