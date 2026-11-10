/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningWorker_Request.cs
    文件功能描述：微调 Worker 管理请求模型


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.16.4 完善 AIKernel 本地微调 Worker、数据库配置与本地化管理能力

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 enhance Agent Studio, group configuration, and task management interactions

    修改标识：Senparc - 20261106
    修改描述：接收本机 Worker 的目录、端口和自动启动配置

----------------------------------------------------------------*/

using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL;

public sealed class AIFineTuningWorker_CreateOrEditRequest
{
    public int Id { get; set; }

    [Required, RegularExpression("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,49}$")]
    public string Alias { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [Required, StringLength(250)]
    public string Endpoint { get; set; }

    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    public bool Enabled { get; set; }

    [StringLength(500)]
    public string Note { get; set; }

    public bool Managed { get; set; }

    [StringLength(100)]
    public string BindHost { get; set; }

    [Range(0, 65535)]
    public int Port { get; set; } = 8091;

    [StringLength(500)]
    public string ModelsRoot { get; set; }

    [StringLength(500)]
    public string DataRoot { get; set; }

    [StringLength(500)]
    public string PythonPath { get; set; }

    [StringLength(500)]
    public string WorkerProjectPath { get; set; }

    public bool AutoStart { get; set; }
}

public sealed class FineTuningEnabledRequest
{
    public bool Enabled { get; set; }
}
