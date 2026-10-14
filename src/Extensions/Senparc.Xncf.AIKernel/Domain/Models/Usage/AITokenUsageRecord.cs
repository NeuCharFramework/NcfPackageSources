/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AITokenUsageRecord.cs
    文件功能描述：统一 Token 用量记录请求

    创建标识：Senparc - 20261011

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    /// 一次模型调用的统一 Token 用量记录请求。
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
