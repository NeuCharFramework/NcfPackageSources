/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningRuntimeSettings.cs
    文件功能描述：在 NCF 启动后调整微调总开关和主机允许列表


    创建标识：Senparc - 20261106

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.18.0 新增微调运行时配置服务
----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;

namespace Senparc.Xncf.AIKernel.Domain.Services;

/// <summary>
/// Persists Enabled and extra allowlist hosts under App_Data and applies them to the live options object.
/// Environment WorkerApiKeys stay an override. A missing settings file leaves the startup environment unchanged.
/// </summary>
public sealed class FineTuningRuntimeSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly FineTuningWorkerOptions _options;
    private readonly FineTuningSecretStore _secrets;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FineTuningRuntimeSettings> _logger;
    private readonly string[] _environmentHosts;
    private readonly bool _environmentEnabled;
    private readonly string _path;
    private readonly object _gate = new();

    public FineTuningRuntimeSettings(IOptions<FineTuningWorkerOptions> options, FineTuningSecretStore secrets,
        IHostEnvironment environment, ILogger<FineTuningRuntimeSettings> logger)
    {
        _options = options.Value;
        _secrets = secrets;
        _environment = environment;
        _logger = logger;
        _environmentHosts = _options.AllowedHosts ?? [];
        _environmentEnabled = _options.Enabled;
        _path = Path.Combine(_environment.ContentRootPath, "App_Data", "Senparc.Xncf.AIKernel", "fine-tuning", "runtime.json");
    }

    public void ApplyStartup()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
            {
                Debug.WriteLine("[AIKernel] Fine-tuning runtime settings file is absent; startup environment remains in effect.");
                return;
            }

            var state = Read();
            Apply(state);
            _logger.LogInformation("Applied fine-tuning runtime settings from {Path}. Enabled={Enabled}.", _path, _options.Enabled);
            Debug.WriteLine($"[AIKernel] Applied fine-tuning runtime settings. Enabled={_options.Enabled}.");
        }
    }

    public FineTuningRuntimeStatus SetEnabled(bool enabled)
    {
        lock (_gate)
        {
            var state = Current();
            state.Enabled = enabled;
            Write(state);
            Apply(state);
            _logger.LogInformation("Fine-tuning runtime enabled={Enabled}. Settings={Path}.", enabled, _path);
            Debug.WriteLine($"[AIKernel] Fine-tuning runtime enabled={enabled}. Settings={_path}.");
            return StatusUnlocked();
        }
    }

    public void AllowHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || FineTuningWorkerOptions.IsLoopbackHost(host))
            return;
        lock (_gate)
        {
            var state = Current();
            if (state.AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase)
                || _environmentHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
                return;
            state.AllowedHosts.Add(host);
            Write(state);
            Apply(state);
            _logger.LogInformation("Allowed fine-tuning worker host {Host}.", host);
            Debug.WriteLine($"[AIKernel] Allowed fine-tuning worker host {host}.");
        }
    }

    public FineTuningRuntimeStatus GetStatus()
    {
        lock (_gate)
            return StatusUnlocked();
    }

    private FineTuningRuntimeStatus StatusUnlocked() => new()
    {
        Enabled = _options.Enabled,
        SecretDirectory = _secrets.DirectoryPath,
        SettingsPath = _path,
        AllowedHosts = (_options.AllowedHosts ?? []).ToArray(),
        DiscoveredWorkerProject = FineTuningWorkerLaunch.DiscoverProject(_environment.ContentRootPath),
        DefaultDataRoot = FineTuningWorkerLaunch.DefaultDataRoot(_environment.ContentRootPath, "{alias}")
    };

    private RuntimeState Current() => File.Exists(_path) ? Read() : new RuntimeState
    {
        Enabled = _environmentEnabled,
        AllowedHosts = []
    };

    private RuntimeState Read()
    {
        try
        {
            var state = JsonSerializer.Deserialize<RuntimeState>(File.ReadAllText(_path), JsonOptions) ?? new RuntimeState();
            state.AllowedHosts ??= [];
            return state;
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            _logger.LogWarning(exception, "Fine-tuning runtime settings at {Path} could not be read.", _path);
            return new RuntimeState { Enabled = _environmentEnabled, AllowedHosts = [] };
        }
    }

    private void Write(RuntimeState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var persisted = new RuntimeState
        {
            Enabled = state.Enabled,
            AllowedHosts = state.AllowedHosts
                .Where(host => !string.IsNullOrWhiteSpace(host)
                    && !_environmentHosts.Contains(host, StringComparer.OrdinalIgnoreCase)
                    && !FineTuningWorkerOptions.IsLoopbackHost(host))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(persisted, JsonOptions));
        File.Move(temporary, _path, overwrite: true);
    }

    private void Apply(RuntimeState state)
    {
        _options.Enabled = state.Enabled;
        _options.AllowedHosts = _environmentHosts
            .Concat(state.AllowedHosts ?? [])
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed class RuntimeState
    {
        public bool Enabled { get; set; }
        public List<string> AllowedHosts { get; set; } = [];
    }
}
