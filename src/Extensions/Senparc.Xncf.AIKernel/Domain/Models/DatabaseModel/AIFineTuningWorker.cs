/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningWorker.cs
    文件功能描述：微调 Worker 数据模型


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.16.4 完善 AIKernel 本地微调 Worker、数据库配置与本地化管理能力

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 enhance Agent Studio, group configuration, and task management interactions

    修改标识：Senparc - 20261106
    修改描述：保存本机 Worker 的启动配置，密钥仍然不进入数据库

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

    /// <summary>
    /// JSON launch profile for a worker started by NCF. Secrets are stored outside the database.
    /// </summary>
    [MaxLength(2000)]
    public string LaunchProfileJson { get; private set; }

    public AIFineTuningWorker(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note, string launchProfileJson = null)
    {
        Apply(alias, name, endpoint, requestTimeoutSeconds, enabled, note, launchProfileJson);
    }

    public AIFineTuningWorker Update(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note, string launchProfileJson = null)
    {
        Apply(alias, name, endpoint, requestTimeoutSeconds, enabled, note, launchProfileJson);
        return this;
    }

    private void Apply(string alias, string name, string endpoint, int requestTimeoutSeconds, bool enabled, string note, string launchProfileJson)
    {
        Alias = alias;
        Name = name;
        Endpoint = endpoint;
        RequestTimeoutSeconds = requestTimeoutSeconds;
        Enabled = enabled;
        Note = note;
        LaunchProfileJson = string.IsNullOrWhiteSpace(launchProfileJson) ? null : launchProfileJson;
    }
}
