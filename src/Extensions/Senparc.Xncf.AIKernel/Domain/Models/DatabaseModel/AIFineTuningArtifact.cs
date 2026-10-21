/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningArtifact.cs
    文件功能描述：微调产物数据模型

    修改标识：Senparc - 20261021
    修改描述：v0.17.0 记录微调产物并关联文件引用

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

    public AIFineTuningArtifact(string workerAlias, string jobExternalId, string externalId, string name, long bytes)
    {
        Update(workerAlias, jobExternalId, externalId, name, bytes, null, null, null);
    }

    public AIFineTuningArtifact(string workerAlias, string jobExternalId, string externalId, string name, long bytes,
        int? fileId, string storageKey, string contentSha256)
    {
        Update(workerAlias, jobExternalId, externalId, name, bytes, fileId, storageKey, contentSha256);
    }

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
