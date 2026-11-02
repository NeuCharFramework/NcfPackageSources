/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfPreviewTask.cs
    文件功能描述：XNCF 隔离预览任务持久化记录

    创建标识：Senparc - 20260802

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

    修改标识：Senparc - 20261103
    修改描述：v0.41.7 XncfBuilder 预览与隔离开发任务表统一租户底层支持

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.XncfBuilder
{
    [Table(Register.DATABASE_PREFIX + nameof(XncfPreviewTask))]
    [Serializable]
    // 统一使用租户底层支持：多租户启用时本表数据按 TenantId 隔离（与 PromptRange 等 XNCF 模块一致）
    public class XncfPreviewTask : EntityBase<int>
    {
        [Required, MaxLength(128)]
        public string SessionId { get; private set; }

        [Required, MaxLength(256)]
        public string ModuleProjectName { get; private set; }

        [MaxLength(1200)]
        public string SolutionFilePath { get; private set; }

        [Required]
        public XncfPreviewStage Stage { get; private set; }

        [Required]
        public int ProgressPercent { get; private set; }

        [MaxLength(1000)]
        public string StatusMessage { get; private set; }

        public string ErrorMessage { get; private set; }

        [MaxLength(64)]
        public string SourceFingerprint { get; private set; }

        [MaxLength(64)]
        public string ModuleAssemblySha256 { get; private set; }

        public string RecentOutput { get; private set; }

        [Required]
        public DateTime StartedAtUtc { get; private set; }

        [Required]
        public DateTime UpdatedAtUtc { get; private set; }

        public DateTime? CompletedAtUtc { get; private set; }

        private XncfPreviewTask()
        {
        }

        internal XncfPreviewTask(XncfPreviewPersistenceSnapshot snapshot)
        {
            SessionId = snapshot.SessionId;
            Apply(snapshot);
        }

        internal void Apply(XncfPreviewPersistenceSnapshot snapshot)
        {
            ModuleProjectName = snapshot.ModuleProjectName;
            SolutionFilePath = snapshot.SolutionFilePath;
            Stage = snapshot.Stage;
            ProgressPercent = snapshot.ProgressPercent;
            StatusMessage = snapshot.StatusMessage;
            ErrorMessage = snapshot.ErrorMessage;
            SourceFingerprint = snapshot.SourceFingerprint;
            ModuleAssemblySha256 = snapshot.ModuleAssemblySha256;
            RecentOutput = snapshot.RecentOutput;
            StartedAtUtc = snapshot.StartedAt.UtcDateTime;
            UpdatedAtUtc = snapshot.UpdatedAt.UtcDateTime;
            CompletedAtUtc = snapshot.CompletedAt?.UtcDateTime;
        }

        internal void MarkInterrupted(DateTimeOffset interruptedAt, string message)
        {
            Stage = XncfPreviewStage.Interrupted;
            StatusMessage = message;
            ErrorMessage = message;
            UpdatedAtUtc = interruptedAt.UtcDateTime;
            CompletedAtUtc = interruptedAt.UtcDateTime;
        }
    }
}
