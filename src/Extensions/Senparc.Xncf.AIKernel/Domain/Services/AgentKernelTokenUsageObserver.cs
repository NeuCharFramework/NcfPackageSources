/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentKernelTokenUsageObserver.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

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
    /// <summary>
    /// Converts one AgentKernel usage event into an NCF token-usage record.
    /// Missing recorder services are ignored so provider completion is never blocked by telemetry.
    /// </summary>
    /// <param name="usageEvent">The completed or failed provider usage event.</param>
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
