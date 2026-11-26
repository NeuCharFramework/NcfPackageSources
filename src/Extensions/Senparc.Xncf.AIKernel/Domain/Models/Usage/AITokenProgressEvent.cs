/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenProgressEvent.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    /// Identifies the lifecycle state of a monitored AI invocation.
    /// </summary>
    public enum AITokenProgressStatus
    {
        /// <summary>The invocation is still running.</summary>
        Running = 0,
        /// <summary>The invocation completed successfully.</summary>
        Completed = 1,
        /// <summary>
        /// failure
        /// </summary>
        Failed = 2,
    }

    /// <summary>
    /// Carries the latest token and timing progress for one monitored AI invocation.
    /// </summary>
    [Serializable]
    public class AITokenProgressEvent
    {
        /// <summary>Gets or sets the monitored invocation identifier.</summary>
        public Guid RunId { get; set; }

        /// <summary>Gets or sets the configured model alias.</summary>
        public string ModelAlias { get; set; }

        /// <summary>Gets or sets the provider model identifier.</summary>
        public string ModelId { get; set; }

        /// <summary>Gets or sets the current invocation status.</summary>
        public AITokenProgressStatus Status { get; set; } = AITokenProgressStatus.Running;

        /// <summary>Gets or sets the number of input/prompt tokens observed.</summary>
        public long InputTokens { get; set; }

        /// <summary>Gets or sets the number of output/completion tokens observed.</summary>
        public long OutputTokens { get; set; }

        /// <summary>Gets or sets the total token count observed.</summary>
        public long TotalTokens { get; set; }

        /// <summary>Gets or sets a bounded preview of generated output.</summary>
        public string OutputPreview { get; set; }

        /// <summary>Gets or sets elapsed invocation time in milliseconds.</summary>
        public int ElapsedMs { get; set; }

        /// <summary>Gets a value indicating whether the invocation is terminal.</summary>
        public bool IsComplete => Status == AITokenProgressStatus.Completed || Status == AITokenProgressStatus.Failed;

        /// <summary>Gets or sets the failure message, when the invocation failed.</summary>
        public string Error { get; set; }

        /// <summary>Gets or sets the local timestamp at which this progress was observed.</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
