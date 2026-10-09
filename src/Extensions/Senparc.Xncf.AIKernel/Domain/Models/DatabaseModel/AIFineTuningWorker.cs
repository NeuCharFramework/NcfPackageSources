/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningWorker.cs
    文件功能描述：微调 Worker 数据模型


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.16.4 完善 AIKernel 本地微调 Worker、数据库配置与本地化管理能力

----------------------------------------------------------------*/

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIKernel.Models;

/// <summary>
/// A locally managed fine-tuning worker. Authentication material remains outside the database.
/// </summary>
[Table(Register.DATABASE_PREFIX + nameof(AIFineTuningWorker))]
[Serializable]
public class AIFineTuningWorker : EntityBase<int>
{
    [Required, MaxLength(50)]
    public string Alias { get; private set; }

    [Required, MaxLength(100)]
    public string Name { get; private set; }

    [Required, MaxLength(250)]
    public string Endpoint { get; private set; }

    [Required, DefaultValue(30)]
    public int RequestTimeoutSeconds { get; private set; }

    [Required, DefaultValue(false)]
    public bool Enabled { get; private set; }

    [MaxLength(500)]
    public string Note { get; private set; }

    public AIFineTuningWorker(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note)
    {
        Apply(alias, name, endpoint, requestTimeoutSeconds, enabled, note);
    }

    public AIFineTuningWorker Update(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note)
    {
        Apply(alias, name, endpoint, requestTimeoutSeconds, enabled, note);
        return this;
    }

    private void Apply(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note)
    {
        Alias = alias;
        Name = name;
        Endpoint = endpoint;
        RequestTimeoutSeconds = requestTimeoutSeconds;
        Enabled = enabled;
        Note = note;
    }
}
