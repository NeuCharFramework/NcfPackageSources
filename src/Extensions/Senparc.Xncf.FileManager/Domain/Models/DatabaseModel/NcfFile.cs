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
    /// <summary>
    /// Represents one persisted file and its storage, visibility, and resource-scope metadata.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(NcfFile))]
    public class NcfFile : EntityBase<int>
    {
        [Required]
        [MaxLength(250)]
        /// Gets or sets the original client-visible file name.
        public string FileName { get; set; }

        [Required]
        /// Gets or sets the generated storage file name.
        public string StorageFileName { get; set; }

        [Required]
        /// Gets or sets the relative storage path.
        public string FilePath { get; set; }

        /// Gets or sets the stored file size in bytes.
        public long FileSize { get; set; }

        [MaxLength(100)]
        /// Gets or sets the file extension.
        public string FileExtension { get; set; }

        /// Gets or sets the coarse file-type classification.
        public FileType FileType { get; set; }

        [MaxLength(300)]
        /// Gets or sets the administrator-facing file description.
        public string Description { get; set; }

        /// Gets or sets the upload timestamp.
        public DateTime UploadTime { get; set; }

        /// Gets or sets the logical folder identifier.
        public int? FolderId { get; set; }

        /// <summary>
        /// Gets or sets the resource scope governing access and extension policy.
        /// </summary>
        public NcfFileResourceScope ResourceScope { get; set; } = NcfFileResourceScope.KnowledgeBase;

        /// <summary>
        /// Gets or sets the public/private access level.
        /// </summary>
        public NcfFileAccessLevel AccessLevel { get; set; } = NcfFileAccessLevel.Private;

        [MaxLength(200)]
        /// <summary>Gets or sets the MIME content type recorded for the file.</summary>
        public string ContentType { get; set; }

        [MaxLength(64)]
        /// <summary>
        /// Gets or sets the content hash used for integrity and publication checks.
        /// </summary>
        public string ContentHash { get; set; }
    }

    /// <summary>Identifies the resource boundary used by file policy decisions.</summary>
    public enum NcfFileResourceScope
    {
        /// <summary>Knowledge-base source material.</summary>
        KnowledgeBase = 100,

        /// <summary>Public site assets.</summary>
        SiteAsset = 200,

        /// <summary>Private attachment files.</summary>
        PrivateAttachment = 300,

        /// <summary>Skill package and resource files.</summary>
        Skill = 400
    }

    /// <summary>Identifies whether a file is private or publicly accessible.</summary>
    public enum NcfFileAccessLevel
    {
        /// <summary>Accessible only through private authorization.</summary>
        Private = 0,
        /// <summary>Eligible for public access.</summary>
        Public = 100
    }

    /// <summary>Classifies the broad content family of a persisted file.</summary>
    public enum FileType
    {
        /// <summary>Plain text or text-like content.</summary>
        Text = 0,
        /// <summary>Word-processing document.</summary>
        Word = 1,
        /// <summary>Presentation document.</summary>
        PowerPoint = 2,
        /// <summary>Spreadsheet document.</summary>
        Excel = 3,
        /// <summary>Source-code or code-like content.</summary>
        Code = 4,
        /// <summary>Other or unclassified content.</summary>
        Other = 999
    }

}
