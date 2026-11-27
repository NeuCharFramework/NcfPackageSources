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

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

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

    /// <summary>
    /// Creates a dataset record without a FileManager reference.
    /// </summary>
    /// <param name="workerAlias">The fine-tuning Worker alias.</param>
    /// <param name="externalId">The Worker-side dataset identifier.</param>
    /// <param name="name">The dataset display name.</param>
    /// <param name="rows">The dataset row count.</param>
    /// <param name="sha256">The dataset content hash.</param>
    /// <param name="createdUtc">The Worker creation timestamp.</param>
    public AIFineTuningDataset(string workerAlias, string externalId, string name, int rows, string sha256,
        DateTime createdUtc)
    {
        Update(workerAlias, externalId, name, rows, sha256, createdUtc);
    }

    /// <summary>
    /// Creates a dataset record with an optional FileManager file identifier.
    /// </summary>
    /// <param name="workerAlias">The fine-tuning Worker alias.</param>
    /// <param name="externalId">The Worker-side dataset identifier.</param>
    /// <param name="name">The dataset display name.</param>
    /// <param name="rows">The dataset row count.</param>
    /// <param name="sha256">The dataset content hash.</param>
    /// <param name="createdUtc">The Worker creation timestamp.</param>
    /// <param name="fileId">The optional FileManager file identifier.</param>
    public AIFineTuningDataset(string workerAlias, string externalId, string name, int rows, string sha256,
        DateTime createdUtc, int? fileId)
    {
        Update(workerAlias, externalId, name, rows, sha256, createdUtc, fileId);
    }

    /// <summary>
    /// Replaces dataset metadata while preserving the entity identity.
    /// </summary>
    /// <param name="workerAlias">The fine-tuning Worker alias.</param>
    /// <param name="externalId">The Worker-side dataset identifier.</param>
    /// <param name="name">The dataset display name.</param>
    /// <param name="rows">The dataset row count.</param>
    /// <param name="sha256">The dataset content hash.</param>
    /// <param name="createdUtc">The Worker creation timestamp.</param>
    /// <returns>This dataset entity.</returns>
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

    /// <summary>
    /// Replaces dataset metadata and its optional FileManager reference.
    /// </summary>
    /// <param name="workerAlias">The fine-tuning Worker alias.</param>
    /// <param name="externalId">The Worker-side dataset identifier.</param>
    /// <param name="name">The dataset display name.</param>
    /// <param name="rows">The dataset row count.</param>
    /// <param name="sha256">The dataset content hash.</param>
    /// <param name="createdUtc">The Worker creation timestamp.</param>
    /// <param name="fileId">The optional FileManager file identifier.</param>
    /// <returns>This dataset entity.</returns>
    public AIFineTuningDataset Update(string workerAlias, string externalId, string name, int rows, string sha256,
        DateTime createdUtc, int? fileId)
    {
        Update(workerAlias, externalId, name, rows, sha256, createdUtc);
        FileId = fileId;
        return this;
    }
}
