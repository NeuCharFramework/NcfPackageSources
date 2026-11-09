/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RunEntity.cs
    文件功能描述：实体：一次 Live 请求（含 what-if）的运行记录


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Models
{
    /// <summary>
    /// One live request (what-ifs included). The end columns stay empty (FinishedUtc null) until it finishes; a row the
    /// server never finished, because it stopped mid-run, stays that way. Retention blanks Text to "" and PlanJson and
    /// Summary to null after 30 days; the numbers are kept.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + "Run")]
    public class RunEntity : EntityBase<int>
    {
        public string Code { get; set; } = "";
        public string Scene { get; set; } = "";
        public string Text { get; set; } = "";
        public bool WhatIf { get; set; }
        public string? WhatIfDevice { get; set; }
        public string Model { get; set; } = "";

        /// <summary>The usage day (NcbLive:UsageTimeZone) the run counts against.</summary>
        public DateOnly Day { get; set; }

        public DateTime StartedUtc { get; set; }

        public string? Outcome { get; set; }
        public string? PlanJson { get; set; }
        public string? Summary { get; set; }
        public int Calls { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public long LatencyMs { get; set; }
        public DateTime? FinishedUtc { get; set; }
    }
}
