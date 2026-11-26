/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfFolder.cs
    File description: NcfFolder related implementation

    Created by: Senparc - 20260104

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileManager.Domain.Models.DatabaseModel
{

    /// Represents a logical folder used to organize uploaded files.
    [Table(Register.DATABASE_PREFIX + nameof(NcfFolder))]//a prefix is required to prevent system-wide conflicts
    public class NcfFolder : EntityBase<int>
    {
        [Required]
        [MaxLength(200)]
        /// Gets or sets the folder name.
        public string Name { get; set; }

        /// Gets or sets the parent folder identifier, or <see langword="null"/> for a root folder.
        public int? ParentId { get; set; }

        /// Gets or sets the administrator-facing folder description.
        [MaxLength(500)]
        public string Description { get; set; }

        /// Gets or sets the UTC creation time.
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the resource scope governing files in this folder.
        /// </summary>
        public NcfFileResourceScope ResourceScope { get; set; } = NcfFileResourceScope.KnowledgeBase;
    }
}
