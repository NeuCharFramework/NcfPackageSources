/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIFineTuningDataset.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add FileId reference to AIFineTuningDataset and implement FineTuningFileReferenceProvider

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIKernel.Models;

[Table(Register.DATABASE_PREFIX + nameof(AIFineTuningDataset))]
[Serializable]
public class AIFineTuningDataset : EntityBase<int>
{
    [Required, MaxLength(50)]
    public string WorkerAlias { get; private set; }

    [Required, MaxLength(128)]
    public string ExternalId { get; private set; }

    [Required, MaxLength(100)]
    public string Name { get; private set; }

    [Required]
    public int Rows { get; private set; }

    [Required, MaxLength(64)]
    public string Sha256 { get; private set; }

    [Required]
    public DateTime CreatedUtc { get; private set; }

    public int? FileId { get; private set; }

    public AIFineTuningDataset(string workerAlias, string externalId, string name, int rows, string sha256,
        DateTime createdUtc)
    {
        Update(workerAlias, externalId, name, rows, sha256, createdUtc);
    }

    public AIFineTuningDataset(string workerAlias, string externalId, string name, int rows, string sha256,
        DateTime createdUtc, int? fileId)
    {
        Update(workerAlias, externalId, name, rows, sha256, createdUtc, fileId);
    }

    public AIFineTuningDataset Update(string workerAlias, string externalId, string name, int rows, string sha256,
        DateTime createdUtc)
    {
        WorkerAlias = workerAlias;
        ExternalId = externalId;
        Name = name;
        Rows = rows;
        Sha256 = sha256;
        CreatedUtc = createdUtc;
        return this;
    }

    public AIFineTuningDataset Update(string workerAlias, string externalId, string name, int rows, string sha256,
        DateTime createdUtc, int? fileId)
    {
        Update(workerAlias, externalId, name, rows, sha256, createdUtc);
        FileId = fileId;
        return this;
    }
}
