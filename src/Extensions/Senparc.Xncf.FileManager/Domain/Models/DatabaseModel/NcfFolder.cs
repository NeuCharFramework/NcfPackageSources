using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileManager.Domain.Models.DatabaseModel
{

    // Add new folder entity
    [Table(Register.DATABASE_PREFIX + nameof(NcfFolder))]//The prefix must be added to prevent conflicts system-wide.
    public class NcfFolder : EntityBase<int>
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public int? ParentId { get; set; } // Parent folder, null is the root

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    }
}
