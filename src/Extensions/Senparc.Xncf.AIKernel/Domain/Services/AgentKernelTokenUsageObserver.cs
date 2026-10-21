/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentKernelTokenUsageObserver.cs
    文件功能描述：观察 AgentKernel 调用并记录 Token 用量
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 观察 AgentKernel 调用并写入 Token 用量

----------------------------------------------------------------*/

using Senparc.AI.AgentKernel.Usage;
using Senparc.CO2NET;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;
using System;

namespace Senparc.Xncf.AIKernel.Domain.Services;

/// <summary>
/// Bridges AgentKernel provider events into NCF's non-blocking token recorder.
/// </summary>
public sealed class AgentKernelTokenUsageObserver : IAIUsageObserver
{
    public void Observe(AIUsageEvent usageEvent)
    {
        var serviceProvider = SenparcDI.GetServiceProvider(true);
        var recorder = serviceProvider.GetService(typeof(IAITokenUsageRecorder)) as IAITokenUsageRecorder;
        if (recorder is null)
        {
            return;
        }

        var context = usageEvent.Context;
        var tenantId = int.TryParse(context?.TenantId, out var explicitTenantId)
            ? explicitTenantId
            : TenantMonitorScope.GetTenantId(serviceProvider);

        recorder.Record(new AITokenUsageRecord
        {
            ModelAlias = context?.ModelName ?? usageEvent.Model,
            ModelId = context?.ModelId,
            DeploymentName = context?.DeploymentName,
            AiPlatform = context?.Platform ?? Senparc.AI.AiPlatform.UnSet,
            ConfigModelType = ConfigModelType.Chat,
            Snapshot = AIModelService.BuildUsageSnapshot(usageEvent.Usage),
            DurationMs = Math.Max(0, (int)(usageEvent.CompletedAt - usageEvent.StartedAt).TotalMilliseconds),
            Success = usageEvent.Success,
            TenantId = tenantId,
            OccurredAt = usageEvent.StartedAt.UtcDateTime,
            Source = context?.Source ?? usageEvent.Source ?? "AgentKernel",
            ErrorNote = usageEvent.Exception?.Message
        });
    }
}
