/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AIFineTuningWorkerDto.cs
    文件功能描述：微调 Worker 数据传输模型


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.16.4 完善 AIKernel 本地微调 Worker、数据库配置与本地化管理能力

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 enhance Agent Studio, group configuration, and task management interactions

    修改标识：Senparc - 20261106
    修改描述：向管理界面返回本机启动配置和密钥文件路径，不返回密钥内容

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;

public sealed class AIFineTuningWorkerDto : DtoBase
{
    public int Id { get; set; }
    public string Alias { get; set; }
    public string Name { get; set; }
    public string Endpoint { get; set; }
    public int RequestTimeoutSeconds { get; set; }
    public bool Enabled { get; set; }
    public string Note { get; set; }
    public bool HasConfiguredSecret { get; set; }
    public bool Managed { get; set; }
    public string BindHost { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8091;
    public string ModelsRoot { get; set; } = "";
    public string DataRoot { get; set; } = "";
    public string PythonPath { get; set; } = "";
    public string WorkerProjectPath { get; set; } = "";
    public bool AutoStart { get; set; }
    public string SecretPath { get; set; } = "";

    public AIFineTuningWorkerDto()
    {
    }

    public AIFineTuningWorkerDto(AIFineTuningWorker worker, bool hasConfiguredSecret)
    {
        Id = worker.Id;
        Alias = worker.Alias;
        Name = worker.Name;
        Endpoint = worker.Endpoint;
        RequestTimeoutSeconds = worker.RequestTimeoutSeconds;
        Enabled = worker.Enabled;
        Note = worker.Note;
        HasConfiguredSecret = hasConfiguredSecret;
        FineTuningLaunchProfile.TryFromJson(worker.LaunchProfileJson, out var profile);
        Managed = profile.Managed;
        BindHost = profile.BindHost;
        Port = profile.Port;
        ModelsRoot = profile.ModelsRoot;
        DataRoot = profile.DataRoot;
        PythonPath = profile.PythonPath;
        WorkerProjectPath = profile.WorkerProjectPath;
        AutoStart = profile.AutoStart;
        AddTime = worker.AddTime;
        LastUpdateTime = worker.LastUpdateTime;
        TenantId = worker.TenantId;
    }
}
