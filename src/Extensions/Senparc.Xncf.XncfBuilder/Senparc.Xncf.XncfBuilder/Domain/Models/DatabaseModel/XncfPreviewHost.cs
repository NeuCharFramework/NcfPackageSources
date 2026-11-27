/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfPreviewHost.cs
    File description: Related implementation.

    Created by: Senparc - 20260802

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.41.7 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.XncfBuilder
{
    [Table(Register.DATABASE_PREFIX + nameof(XncfPreviewHost))]
    [Serializable]
    //Legacy comment translated to English.
    public class XncfPreviewHost : EntityBase<int>
    {
        [Required, MaxLength(128)]
        public string SessionId { get; private set; }

        [Required, MaxLength(256)]
        public string ModuleProjectName { get; private set; }

        [MaxLength(500)]
        public string Url { get; private set; }

        public int ProcessId { get; private set; }

        [MaxLength(100)]
        public string EnvironmentName { get; private set; }

        [MaxLength(1200)]
        public string PublishDirectory { get; private set; }

        [Required]
        public XncfPreviewHostStatus Status { get; private set; }

        [MaxLength(1000)]
        public string StatusMessage { get; private set; }

        public DateTime? ProcessStartedAtUtc { get; private set; }

        public DateTime? HealthyAtUtc { get; private set; }

        public DateTime? StoppedAtUtc { get; private set; }

        public int? ExitCode { get; private set; }

        [Required]
        public DateTime UpdatedAtUtc { get; private set; }

        /// <summary>
        /// Initializes an empty preview-host entity for ORM materialization.
        /// </summary>
        private XncfPreviewHost()
        {
        }

        /// <summary>
        /// Initializes a preview-host entity from a persistence snapshot.
        /// </summary>
        /// <param name="snapshot">The source persistence snapshot.</param>
        internal XncfPreviewHost(XncfPreviewPersistenceSnapshot snapshot)
        {
            SessionId = snapshot.SessionId;
            Apply(snapshot);
        }

        /// <summary>
        /// Applies snapshot state to the persisted host entity.
        /// </summary>
        /// <param name="snapshot">The source persistence snapshot.</param>
        internal void Apply(XncfPreviewPersistenceSnapshot snapshot)
        {
            ModuleProjectName = snapshot.ModuleProjectName;
            Url = snapshot.Url;
            ProcessId = snapshot.ProcessId;
            EnvironmentName = snapshot.EnvironmentName;
            PublishDirectory = snapshot.PublishDirectory;
            Status = snapshot.HostStatus;
            StatusMessage = snapshot.HostStatusMessage;
            ProcessStartedAtUtc = snapshot.ProcessStartedAt?.UtcDateTime;
            HealthyAtUtc = snapshot.HealthyAt?.UtcDateTime;
            StoppedAtUtc = snapshot.StoppedAt?.UtcDateTime;
            ExitCode = snapshot.ExitCode;
            UpdatedAtUtc = snapshot.UpdatedAt.UtcDateTime;
        }

        /// <summary>
        /// Marks the host as interrupted after an application restart or recovery boundary.
        /// </summary>
        /// <param name="interruptedAt">The interruption timestamp.</param>
        /// <param name="message">The interruption status message.</param>
        internal void MarkInterrupted(DateTimeOffset interruptedAt, string message)
        {
            Status = XncfPreviewHostStatus.Interrupted;
            StatusMessage = message;
            StoppedAtUtc = interruptedAt.UtcDateTime;
            UpdatedAtUtc = interruptedAt.UtcDateTime;
        }
    }
}
