/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIFineTuningMigration.cs
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

[Table(Register.DATABASE_PREFIX + nameof(AIFineTuningMigration))]
[Serializable]
public class AIFineTuningMigration : EntityBase<int>
{
    [Required, MaxLength(50)]
    public string WorkerAlias { get; private set; }

    [Required, MaxLength(128)]
    public string StoreId { get; private set; }

    [Required, MaxLength(100)]
    public string MigrationKey { get; private set; }

    [Required, MaxLength(50)]
    public string Status { get; private set; }

    [MaxLength(100)]
    public string SourceVersion { get; private set; }

    [MaxLength(100)]
    public string TargetVersion { get; private set; }

    [Required]
    public DateTime StartedUtc { get; private set; }

    public DateTime? CompletedUtc { get; private set; }

    public string Error { get; private set; }

    /// <summary>
    /// Creates a persisted Worker-store migration record.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="storeId">The Worker store identifier.</param>
    /// <param name="migrationKey">The migration key.</param>
    /// <param name="status">The migration status.</param>
    /// <param name="sourceVersion">The source version.</param>
    /// <param name="targetVersion">The target version.</param>
    /// <param name="startedUtc">The start timestamp.</param>
    /// <param name="completedUtc">The optional completion timestamp.</param>
    /// <param name="error">The optional failure message.</param>
    public AIFineTuningMigration(string workerAlias, string storeId, string migrationKey, string status,
        string sourceVersion, string targetVersion, DateTime startedUtc, DateTime? completedUtc, string error)
    {
        Update(workerAlias, storeId, migrationKey, status, sourceVersion, targetVersion, startedUtc, completedUtc, error);
    }

    /// <summary>
    /// Updates the persisted migration state.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="storeId">The Worker store identifier.</param>
    /// <param name="migrationKey">The migration key.</param>
    /// <param name="status">The migration status.</param>
    /// <param name="sourceVersion">The source version.</param>
    /// <param name="targetVersion">The target version.</param>
    /// <param name="startedUtc">The start timestamp.</param>
    /// <param name="completedUtc">The optional completion timestamp.</param>
    /// <param name="error">The optional failure message.</param>
    /// <returns>This migration entity.</returns>
    public AIFineTuningMigration Update(string workerAlias, string storeId, string migrationKey, string status,
        string sourceVersion, string targetVersion, DateTime startedUtc, DateTime? completedUtc, string error)
    {
        WorkerAlias = workerAlias;
        StoreId = storeId;
        MigrationKey = migrationKey;
        Status = status;
        SourceVersion = sourceVersion;
        TargetVersion = targetVersion;
        StartedUtc = startedUtc;
        CompletedUtc = completedUtc;
        Error = error;
        return this;
    }
}
