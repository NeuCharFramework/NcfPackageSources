/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenUsageRecord.cs
    File description: Related implementation.

    Created by: Senparc - 20261011

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    /// Captures normalized token usage and invocation metadata before persistence.
    /// </summary>
    [Serializable]
    public sealed class AITokenUsageRecord
    {
        /// <summary>Gets or initializes the configured model alias.</summary>
        public string ModelAlias { get; init; }
        /// <summary>Gets or initializes the provider model identifier.</summary>
        public string ModelId { get; init; }
        /// <summary>Gets or initializes the provider deployment name.</summary>
        public string DeploymentName { get; init; }
        /// <summary>Gets or initializes the AI platform.</summary>
        public AiPlatform AiPlatform { get; init; }
        /// <summary>Gets or initializes the model capability category.</summary>
        public ConfigModelType ConfigModelType { get; init; }
        /// <summary>Gets or initializes the normalized token snapshot.</summary>
        public AITokenUsageSnapshot Snapshot { get; init; }
        /// <summary>Gets or initializes invocation duration in milliseconds.</summary>
        public int DurationMs { get; init; }
        /// <summary>Gets or initializes whether the invocation succeeded.</summary>
        public bool Success { get; init; }
        /// <summary>Gets or initializes the tenant identifier associated with the invocation.</summary>
        public int TenantId { get; init; }
        /// <summary>Gets or initializes the UTC occurrence timestamp.</summary>
        public DateTime OccurredAt { get; init; }
        /// <summary>Gets or initializes the logical source of the invocation.</summary>
        public string Source { get; init; }
        /// <summary>Gets or initializes the bounded error description.</summary>
        public string ErrorNote { get; init; }
    }
}
