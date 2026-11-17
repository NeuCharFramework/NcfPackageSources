/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: XncfPreviewTask.cs
    File description: Related implementation.

    Created by: Senparc - 20260802

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.41.7 Updated module behavior and related support.

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
    //Legacy comment translated to English.
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
