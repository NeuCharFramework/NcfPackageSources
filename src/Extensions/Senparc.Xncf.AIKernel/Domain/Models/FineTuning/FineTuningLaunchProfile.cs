/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningLaunchProfile.cs
    文件功能描述：微调 Worker 的本机启动配置


    创建标识：Senparc - 20261106

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.18.0 新增微调 Worker 启动配置模型
----------------------------------------------------------------*/

using System;
using System.Text.Json;

namespace Senparc.Xncf.AIKernel.Domain.Models.FineTuning;

/// <summary>
/// Launch settings stored with a worker. Authentication material is never part of this profile.
/// </summary>
public sealed class FineTuningLaunchProfile
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool Managed { get; set; }
    public string BindHost { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8091;
    public string ModelsRoot { get; set; } = "";
    public string DataRoot { get; set; } = "";
    public string PythonPath { get; set; } = "";
    public string WorkerProjectPath { get; set; } = "";
    public bool AutoStart { get; set; }

    public string ToJson()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        if (json.Length > 2000)
            throw new InvalidOperationException("The fine-tuning launch profile is too long to store.");
        return json;
    }

    public static bool TryFromJson(string json, out FineTuningLaunchProfile profile)
    {
        profile = new FineTuningLaunchProfile();
        if (string.IsNullOrWhiteSpace(json))
            return true;
        try
        {
            profile = JsonSerializer.Deserialize<FineTuningLaunchProfile>(json, JsonOptions) ?? new FineTuningLaunchProfile();
            profile.BindHost = string.IsNullOrWhiteSpace(profile.BindHost) ? "127.0.0.1" : profile.BindHost;
            profile.ModelsRoot ??= "";
            profile.DataRoot ??= "";
            profile.PythonPath ??= "";
            profile.WorkerProjectPath ??= "";
            if (profile.Port == 0)
                profile.Port = 8091;
            return true;
        }
        catch (JsonException)
        {
            profile = new FineTuningLaunchProfile();
            return false;
        }
    }
}

public sealed class FineTuningRuntimeStatus
{
    public bool Enabled { get; set; }
    public string SecretDirectory { get; set; } = "";
    public string SettingsPath { get; set; } = "";
    public string[] AllowedHosts { get; set; } = [];
    public string DiscoveredWorkerProject { get; set; } = "";
    public string DefaultDataRoot { get; set; } = "";
}

public sealed class FineTuningSecretStatus
{
    public string Alias { get; set; } = "";
    public bool Configured { get; set; }
    public string Path { get; set; } = "";
    public bool Created { get; set; }
}

public sealed class FineTuningManagedWorkerStatus
{
    public string Alias { get; set; } = "";
    public string State { get; set; } = "Stopped";
    public int? ProcessId { get; set; }
    public DateTimeOffset? StartedUtc { get; set; }
    public int? ExitCode { get; set; }
    public string Error { get; set; }
    public string Endpoint { get; set; } = "";
    public string ModelsRoot { get; set; } = "";
    public string DataRoot { get; set; } = "";
    public string BindHost { get; set; } = "";
    public int Port { get; set; }
    public string PythonPath { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public string SecretPath { get; set; } = "";
    public bool SecretConfigured { get; set; }
    public string CommandPreview { get; set; } = "";
    public string[] RecentLogs { get; set; } = [];
}
