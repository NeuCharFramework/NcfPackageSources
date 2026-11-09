/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SettingsEntity.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Models）：SettingsEntity


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.2.0-preview1 新增设置表实体

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    /// <summary>
    /// The module's one settings row (admin-edited; no row means the defaults). Times are UTC. ExtraOrigins holds the
    /// extra CORS origins, one per line, already normalized.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + "Settings")]
    public class SettingsEntity : EntityBase<int>
    {
        public bool Enabled { get; set; } = true;

        /// <summary>The AI Kernel model Id the planner uses; null = not chosen.</summary>
        public int? PlanModelId { get; set; }

        /// <summary>The AI Kernel model Id the runner uses; null = not chosen.</summary>
        public int? RunModelId { get; set; }

        public int PerCodeDaily { get; set; }
        public int GlobalDaily { get; set; }
        public long DailyTokenBudget { get; set; }

        /// <summary>One origin per line.</summary>
        public string ExtraOrigins { get; set; } = "";

        public DateTime UpdatedUtc { get; set; }
    }
}
