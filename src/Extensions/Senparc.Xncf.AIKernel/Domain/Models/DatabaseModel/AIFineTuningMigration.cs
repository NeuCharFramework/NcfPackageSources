/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningMigration.cs
    文件功能描述：微调数据迁移记录模型

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

    public AIFineTuningMigration(string workerAlias, string storeId, string migrationKey, string status,
        string sourceVersion, string targetVersion, DateTime startedUtc, DateTime? completedUtc, string error)
    {
        Update(workerAlias, storeId, migrationKey, status, sourceVersion, targetVersion, startedUtc, completedUtc, error);
    }

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
