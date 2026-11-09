/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：InviteCodeEntity.cs
    文件功能描述：实体：邀请码（NCB-XXXX-XXXX）及其标签、每日限额与停用状态


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    /// <summary>
    /// An invite code (spec 7.1). AddTime is when it was created, in UTC (this module stores every time in UTC);
    /// uses today come from the run log, not from here.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + "InviteCode")]
    public class InviteCodeEntity : EntityBase<int>
    {
        /// <summary>Canonical form, e.g. NCB-7KQ4-M2XD.</summary>
        public string Code { get; set; } = "";

        /// <summary><see cref="Senparc.Xncf.NeuCharBoxLive.Engine.Access.InviteCodes.Normalize"/> of Code; unique.</summary>
        public string Normalized { get; set; } = "";

        /// <summary>Who it is for: the applicant's email for an approved application.</summary>
        public string Label { get; set; } = "";

        public int DailyLimit { get; set; }
        public bool Disabled { get; set; }
        public int UsesTotal { get; set; }
        public DateTime? LastUsedUtc { get; set; }
    }
}
