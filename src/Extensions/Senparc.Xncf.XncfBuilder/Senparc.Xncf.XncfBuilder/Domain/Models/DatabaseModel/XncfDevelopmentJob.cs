/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentJob.cs
    文件功能描述：XNCF 隔离开发、验证及人工合入任务持久化记录

    创建标识：Senparc - 20260814

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.XncfBuilder.Domain.Services.Development;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.XncfBuilder
{
    [Table(Register.DATABASE_PREFIX + nameof(XncfDevelopmentJob))]
    [Serializable]
    public class XncfDevelopmentJob : EntityBase<int>, IIgnoreMulitTenant
    {
        [Required, MaxLength(64)]
        public string JobId { get; private set; }

        [Required]
        public XncfDevelopmentJobMode Mode { get; private set; }

        [Required, MaxLength(256)]
        public string ModuleProjectName { get; private set; }

        [Required, MaxLength(1200)]
        public string TargetSolutionFilePath { get; private set; }

        [MaxLength(1200)]
        public string WorkspaceRootPath { get; private set; }

        [MaxLength(1200)]
        public string WorkspaceSolutionFilePath { get; private set; }

        [MaxLength(4000)]
        public string Requirement { get; private set; }

        [Required]
        public XncfDevelopmentJobStage Stage { get; private set; }

        [MaxLength(1000)]
        public string StatusMessage { get; private set; }

        public string ErrorMessage { get; private set; }

        [MaxLength(64)]
        public string TargetModuleFingerprint { get; private set; }

        [MaxLength(64)]
        public string WorkspaceModuleFingerprint { get; private set; }

        public string ValidationSummary { get; private set; }

        public string DiffSummary { get; private set; }

        [MaxLength(128)]
        public string PreviewSessionId { get; private set; }

        [MaxLength(64)]
        public string SandboxSessionId { get; private set; }

        [MaxLength(500)]
        public string PreviewUrl { get; private set; }

        [Required]
        public DateTime CreatedAtUtc { get; private set; }

        [Required]
        public DateTime UpdatedAtUtc { get; private set; }

        public DateTime? CompletedAtUtc { get; private set; }

        public DateTime? MergeRequestedAtUtc { get; private set; }

        public DateTime? AppliedAtUtc { get; private set; }

        private XncfDevelopmentJob()
        {
        }

        internal XncfDevelopmentJob(XncfDevelopmentJobSnapshot snapshot)
        {
            JobId = snapshot.JobId;
            Apply(snapshot);
        }

        internal void Apply(XncfDevelopmentJobSnapshot snapshot)
        {
            Mode = snapshot.Mode;
            ModuleProjectName = snapshot.ModuleProjectName;
            TargetSolutionFilePath = snapshot.TargetSolutionFilePath;
            WorkspaceRootPath = snapshot.WorkspaceRootPath;
            WorkspaceSolutionFilePath = snapshot.WorkspaceSolutionFilePath;
            Requirement = snapshot.Requirement;
            Stage = snapshot.Stage;
            StatusMessage = snapshot.StatusMessage;
            ErrorMessage = snapshot.ErrorMessage;
            TargetModuleFingerprint = snapshot.TargetModuleFingerprint;
            WorkspaceModuleFingerprint = snapshot.WorkspaceModuleFingerprint;
            ValidationSummary = snapshot.ValidationSummary;
            DiffSummary = snapshot.DiffSummary;
            PreviewSessionId = snapshot.PreviewSessionId;
            SandboxSessionId = snapshot.SandboxSessionId;
            PreviewUrl = snapshot.PreviewUrl;
            CreatedAtUtc = snapshot.CreatedAt.UtcDateTime;
            UpdatedAtUtc = snapshot.UpdatedAt.UtcDateTime;
            CompletedAtUtc = snapshot.CompletedAt?.UtcDateTime;
            MergeRequestedAtUtc = snapshot.MergeRequestedAt?.UtcDateTime;
            AppliedAtUtc = snapshot.AppliedAt?.UtcDateTime;
        }
    }
}
