/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningEvent.cs
    文件功能描述：微调事件数据模型

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIKernel.Models;

[Table(Register.DATABASE_PREFIX + nameof(AIFineTuningEvent))]
[Serializable]
public class AIFineTuningEvent : EntityBase<int>
{
    [Required, MaxLength(50)]
    public string WorkerAlias { get; private set; }

    [Required, MaxLength(128)]
    public string JobExternalId { get; private set; }

    [Required]
    public long Sequence { get; private set; }

    [Required]
    public DateTime TimestampUtc { get; private set; }

    [Required, MaxLength(50)]
    public string Kind { get; private set; }

    public string Message { get; private set; }

    public string MetricsJson { get; private set; }

    public AIFineTuningEvent(string workerAlias, string jobExternalId, long sequence, DateTime timestampUtc,
        string kind, string message, string metricsJson)
    {
        Update(workerAlias, jobExternalId, sequence, timestampUtc, kind, message, metricsJson);
    }

    public AIFineTuningEvent Update(string workerAlias, string jobExternalId, long sequence, DateTime timestampUtc,
        string kind, string message, string metricsJson)
    {
        WorkerAlias = workerAlias;
        JobExternalId = jobExternalId;
        Sequence = sequence;
        TimestampUtc = timestampUtc;
        Kind = kind;
        Message = message;
        MetricsJson = metricsJson;
        return this;
    }
}
