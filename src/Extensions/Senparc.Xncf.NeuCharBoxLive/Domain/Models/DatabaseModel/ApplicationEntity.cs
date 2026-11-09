/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ApplicationEntity.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：ApplicationEntity


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    /// <summary>An application from the demo's apply form (spec 7.2).</summary>
    [Table(Register.DATABASE_PREFIX + "Application")]
    public class ApplicationEntity : EntityBase<int>
    {
        public string Email { get; set; } = "";
        public string? Name { get; set; }
        public string Interest { get; set; } = "";
        public string WouldTry { get; set; } = "";
        public string Scene { get; set; } = "";
        public DateTime SubmittedUtc { get; set; }

        /// <summary>"new", "approved" or "declined".</summary>
        public string Status { get; set; } = "new";

        /// <summary>The code issued. Stored as given, with no foreign key: a code may be missing (or be another store's).</summary>
        public int? CodeId { get; set; }
    }
}
