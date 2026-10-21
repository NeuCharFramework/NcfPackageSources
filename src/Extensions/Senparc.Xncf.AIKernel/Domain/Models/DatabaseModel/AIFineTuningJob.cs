/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningJob.cs
    文件功能描述：微调任务数据模型

    修改标识：Senparc - 20261021
    修改描述：v0.17.0 记录微调任务状态与执行参数

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIKernel.Models;

[Table(Register.DATABASE_PREFIX + nameof(AIFineTuningJob))]
[Serializable]
public class AIFineTuningJob : EntityBase<int>
{
    [Required, MaxLength(50)]
    public string WorkerAlias { get; private set; }

    [Required, MaxLength(128)]
    public string ExternalId { get; private set; }

    [Required, MaxLength(100)]
    public string Name { get; private set; }

    [Required, MaxLength(50)]
    public string State { get; private set; }

    [Required]
    public DateTime CreatedUtc { get; private set; }

    public DateTime? StartedUtc { get; private set; }

    public DateTime? FinishedUtc { get; private set; }

    [Required]
    public string RequestJson { get; private set; }

    public string Error { get; private set; }

    public string LatestMetricsJson { get; private set; }

    [Required]
    public long LastEventSequence { get; private set; }

    public AIFineTuningJob(string workerAlias, string externalId, string name, string state, DateTime createdUtc,
        DateTime? startedUtc, DateTime? finishedUtc, string requestJson, string error, string latestMetricsJson,
        long lastEventSequence)
    {
        Update(workerAlias, externalId, name, state, createdUtc, startedUtc, finishedUtc, requestJson, error,
            latestMetricsJson, lastEventSequence);
    }

    public AIFineTuningJob Update(string workerAlias, string externalId, string name, string state, DateTime createdUtc,
        DateTime? startedUtc, DateTime? finishedUtc, string requestJson, string error, string latestMetricsJson,
        long lastEventSequence)
    {
        WorkerAlias = workerAlias;
        ExternalId = externalId;
        Name = name;
        State = state;
        CreatedUtc = createdUtc;
        StartedUtc = startedUtc;
        FinishedUtc = finishedUtc;
        RequestJson = requestJson;
        Error = error;
        LatestMetricsJson = latestMetricsJson;
        LastEventSequence = lastEventSequence;
        return this;
    }
}
