using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileManager.Domain.Models.DatabaseModel
{

    // 新增文件夹实体
    [Table(Register.DATABASE_PREFIX + nameof(NcfFolder))]//必须添加前缀，防止全系统中发生冲突
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
