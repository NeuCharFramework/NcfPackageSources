/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIFineTuningArtifact.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

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

[Table(Register.DATABASE_PREFIX + nameof(AIFineTuningArtifact))]
[Serializable]
public class AIFineTuningArtifact : EntityBase<int>
{
    [Required, MaxLength(50)]
    public string WorkerAlias { get; private set; }

    [Required, MaxLength(128)]
    public string JobExternalId { get; private set; }

    [Required, MaxLength(128)]
    public string ExternalId { get; private set; }

    [Required, MaxLength(255)]
    public string Name { get; private set; }

    [Required]
    public long Bytes { get; private set; }

    public int? FileId { get; private set; }

    [MaxLength(512)]
    public string StorageKey { get; private set; }

    [MaxLength(128)]
    public string ContentSha256 { get; private set; }

    /// <summary>
    /// Creates an artifact record without a FileManager reference.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="jobExternalId">The Worker-side job identifier.</param>
    /// <param name="externalId">The Worker-side artifact identifier.</param>
    /// <param name="name">The artifact name.</param>
    /// <param name="bytes">The artifact size.</param>
    public AIFineTuningArtifact(string workerAlias, string jobExternalId, string externalId, string name, long bytes)
    {
        Update(workerAlias, jobExternalId, externalId, name, bytes, null, null, null);
    }

    /// <summary>
    /// Creates an artifact record with FileManager storage metadata.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="jobExternalId">The Worker-side job identifier.</param>
    /// <param name="externalId">The Worker-side artifact identifier.</param>
    /// <param name="name">The artifact name.</param>
    /// <param name="bytes">The artifact size.</param>
    /// <param name="fileId">The optional FileManager file identifier.</param>
    /// <param name="storageKey">The optional storage key.</param>
    /// <param name="contentSha256">The optional content hash.</param>
    public AIFineTuningArtifact(string workerAlias, string jobExternalId, string externalId, string name, long bytes,
        int? fileId, string storageKey, string contentSha256)
    {
        Update(workerAlias, jobExternalId, externalId, name, bytes, fileId, storageKey, contentSha256);
    }

    /// <summary>
    /// Updates persisted artifact metadata.
    /// </summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="jobExternalId">The Worker-side job identifier.</param>
    /// <param name="externalId">The Worker-side artifact identifier.</param>
    /// <param name="name">The artifact name.</param>
    /// <param name="bytes">The artifact size.</param>
    /// <param name="fileId">The optional FileManager file identifier.</param>
    /// <param name="storageKey">The optional storage key.</param>
    /// <param name="contentSha256">The optional content hash.</param>
    /// <returns>This artifact entity.</returns>
    public AIFineTuningArtifact Update(string workerAlias, string jobExternalId, string externalId, string name, long bytes,
        int? fileId, string storageKey, string contentSha256)
    {
        WorkerAlias = workerAlias;
        JobExternalId = jobExternalId;
        ExternalId = externalId;
        Name = name;
        Bytes = bytes;
        FileId = fileId;
        StorageKey = storageKey;
        ContentSha256 = contentSha256;
        return this;
    }
}
