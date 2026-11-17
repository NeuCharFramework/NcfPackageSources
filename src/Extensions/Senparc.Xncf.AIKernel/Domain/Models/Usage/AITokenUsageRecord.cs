/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenUsageRecord.cs
    File description: Related implementation.

    Created by: Senparc - 20261011

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public sealed class AITokenUsageRecord
    {
        public string ModelAlias { get; init; }
        public string ModelId { get; init; }
        public string DeploymentName { get; init; }
        public AiPlatform AiPlatform { get; init; }
        public ConfigModelType ConfigModelType { get; init; }
        public AITokenUsageSnapshot Snapshot { get; init; }
        public int DurationMs { get; init; }
        public bool Success { get; init; }
        public int TenantId { get; init; }
        public DateTime OccurredAt { get; init; }
        public string Source { get; init; }
        public string ErrorNote { get; init; }
    }
}
