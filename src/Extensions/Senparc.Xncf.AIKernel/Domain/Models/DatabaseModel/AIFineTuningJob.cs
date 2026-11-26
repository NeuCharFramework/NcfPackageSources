/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIFineTuningJob.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

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

    /// <summary>
    /// Creates a persisted fine-tuning job snapshot.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="externalId">The Worker-side job identifier.</param>
    /// <param name="name">The job display name.</param>
    /// <param name="state">The current job state.</param>
    /// <param name="createdUtc">The creation timestamp.</param>
    /// <param name="startedUtc">The optional start timestamp.</param>
    /// <param name="finishedUtc">The optional completion timestamp.</param>
    /// <param name="requestJson">The serialized job request.</param>
    /// <param name="error">The latest error message.</param>
    /// <param name="latestMetricsJson">The latest metrics JSON.</param>
    /// <param name="lastEventSequence">The latest Worker event sequence.</param>
    public AIFineTuningJob(string workerAlias, string externalId, string name, string state, DateTime createdUtc,
        DateTime? startedUtc, DateTime? finishedUtc, string requestJson, string error, string latestMetricsJson,
        long lastEventSequence)
    {
        Update(workerAlias, externalId, name, state, createdUtc, startedUtc, finishedUtc, requestJson, error,
            latestMetricsJson, lastEventSequence);
    }

    /// <summary>
    /// Updates the persisted job snapshot from Worker state.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="externalId">The Worker-side job identifier.</param>
    /// <param name="name">The job display name.</param>
    /// <param name="state">The current job state.</param>
    /// <param name="createdUtc">The creation timestamp.</param>
    /// <param name="startedUtc">The optional start timestamp.</param>
    /// <param name="finishedUtc">The optional completion timestamp.</param>
    /// <param name="requestJson">The serialized job request.</param>
    /// <param name="error">The latest error message.</param>
    /// <param name="latestMetricsJson">The latest metrics JSON.</param>
    /// <param name="lastEventSequence">The latest Worker event sequence.</param>
    /// <returns>This job entity.</returns>
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
