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

    //Legacy comment translated to English.
    [Table(Register.DATABASE_PREFIX + nameof(NcfFolder))]//a prefix is required to prevent system-wide conflicts
    public class NcfFolder : EntityBase<int>
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public int? ParentId { get; set; } //Legacy comment translated to English.

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public NcfFileResourceScope ResourceScope { get; set; } = NcfFileResourceScope.KnowledgeBase;
    }
}
