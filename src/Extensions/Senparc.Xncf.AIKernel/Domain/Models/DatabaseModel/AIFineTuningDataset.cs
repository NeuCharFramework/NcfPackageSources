/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningDataset.cs
    文件功能描述：微调数据集数据模型

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

    public AIFineTuningDataset(string workerAlias, string externalId, string name, int rows, string sha256,
        DateTime createdUtc)
    {
        Update(workerAlias, externalId, name, rows, sha256, createdUtc);
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
}
