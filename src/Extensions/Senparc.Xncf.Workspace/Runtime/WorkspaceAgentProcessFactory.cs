/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceAgentProcessFactory.cs
    文件功能描述：创建跨平台原生或 Docker Workspace Agent 进程

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Ncf.IsolatedRuntime;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Xncf.Workspace;

public sealed record WorkspaceRuntimeStatus(
    string Provider,
    string IsolationLevel,
    bool ExecutionEnabled,
    bool IsContainerized);

public sealed class WorkspaceAgentProcessFactory
{
    private readonly WorkspaceOptions _options;
    private readonly ILogger<WorkspaceAgentProcessFactory> _logger;
    private readonly IIsolatedRuntimeProvider? _dockerProvider;

    public WorkspaceAgentProcessFactory(
        IOptions<WorkspaceOptions> options,
        ILogger<WorkspaceAgentProcessFactory> logger,
        IEnumerable<IIsolatedRuntimeProvider>? isolatedRuntimeProviders = null)
    {
        _options = options.Value;
        _logger = logger;
        _dockerProvider = isolatedRuntimeProviders?.SingleOrDefault(provider =>
            string.Equals(provider.ProviderName, "Docker", StringComparison.OrdinalIgnoreCase));
    }

    public WorkspaceRuntimeStatus Status
    {
        get
        {
            var containerized = string.Equals(
                _options.RuntimeProvider,
                "Docker",
                StringComparison.OrdinalIgnoreCase);
            return new WorkspaceRuntimeStatus(
                containerized ? "Docker" : "NativeAgent",
                containerized ? "Container" : "NativeFileAccess",
                containerized || _options.AllowNativeExecution,
                containerized);
        }
    }

    public IIsolatedRuntimeConnection Start(string workspaceId, string workspaceRoot)
    {
        var process = Status.IsContainerized
            ? StartDocker(workspaceId, workspaceRoot)
            : StartNative(workspaceId, workspaceRoot);
        _logger.LogInformation(
            "Workspace Agent started: {WorkspaceId}, Provider={Provider}, ProcessId={ProcessId}",
            workspaceId,
            Status.Provider,
            process.ProcessId);
        return process;
    }

    private IIsolatedRuntimeConnection StartNative(string workspaceId, string workspaceRoot)
    {
        var agentPath = ResolveAgentPath();
        var startInfo = CreateBaseStartInfo("dotnet");
        startInfo.ArgumentList.Add(agentPath);
        AddAgentArguments(
            startInfo.ArgumentList,
            workspaceRoot,
            _options.AllowNativeExecution,
            "NativeFileAccess");
        return StartProcess(workspaceId, startInfo);
    }

    private IIsolatedRuntimeConnection StartDocker(string workspaceId, string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(_options.DockerImage))
        {
            throw new InvalidOperationException("Workspace Docker image is not configured.");
        }

        var provider = _dockerProvider
                       ?? throw new InvalidOperationException(
                           "Docker isolated runtime provider is not registered.");
        var command = new List<string>();
        AddAgentArguments(command, "/workspace", true, "Container");
        return provider.StartAttached(new IsolatedRuntimeSpec
        {
            Image = _options.DockerImage,
            Name = $"ncf-workspace-{workspaceId}",
            Command = command,
            AutoRemove = true,
            OpenStandardInput = true,
            NetworkMode = "none",
            ReadOnlyRootFileSystem = true,
            DropAllCapabilities = true,
            NoNewPrivileges = true,
            CpuLimit = Math.Max(0.25, _options.DockerCpuLimit),
            MemoryMegabytes = Math.Max(128, _options.DockerMemoryMegabytes),
            PidsLimit = 128,
            Labels = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ncf.workspace"] = "1",
                ["ncf.workspace.id"] = workspaceId
            },
            Mounts =
            [
                new IsolatedRuntimeMount(Path.GetFullPath(workspaceRoot), "/workspace")
            ]
        });
    }

    private static ProcessStartInfo CreateBaseStartInfo(string fileName)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private void AddAgentArguments(
        ICollection<string> arguments,
        string rootPath,
        bool executionEnabled,
        string isolationLevel)
    {
        Add("--root", rootPath);
        Add("--max-workspace-bytes", _options.MaxWorkspaceBytes.ToString(CultureInfo.InvariantCulture));
        Add("--max-entries", _options.MaxEntriesPerList.ToString(CultureInfo.InvariantCulture));
        Add("--max-read-bytes", _options.MaxFileReadBytes.ToString(CultureInfo.InvariantCulture));
        Add("--max-write-bytes", _options.MaxFileWriteBytes.ToString(CultureInfo.InvariantCulture));
        Add("--max-search-file-bytes", _options.MaxSearchFileBytes.ToString(CultureInfo.InvariantCulture));
        Add("--max-search-files", _options.MaxSearchFiles.ToString(CultureInfo.InvariantCulture));
        Add("--max-search-results", _options.MaxSearchResults.ToString(CultureInfo.InvariantCulture));
        Add("--command-timeout-seconds", _options.CommandTimeoutSeconds.ToString(CultureInfo.InvariantCulture));
        Add("--max-command-output-bytes", _options.MaxCommandOutputBytes.ToString(CultureInfo.InvariantCulture));
        Add("--execution-enabled", executionEnabled.ToString(CultureInfo.InvariantCulture));
        Add("--isolation-level", isolationLevel);
        Add("--allowed-commands", string.Join(',', _options.AllowedCommands ?? Array.Empty<string>()));
        return;

        void Add(string key, string value)
        {
            arguments.Add(key);
            arguments.Add(value);
        }
    }

    private IIsolatedRuntimeConnection StartProcess(string workspaceId, ProcessStartInfo startInfo)
    {
        try
        {
            return ProcessIsolatedRuntimeConnection.Start(startInfo);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Workspace Agent failed to start: {WorkspaceId}, Provider={Provider}",
                workspaceId,
                Status.Provider);
            throw;
        }
    }

    private string ResolveAgentPath()
    {
        var path = string.IsNullOrWhiteSpace(_options.AgentPath)
            ? Path.Combine(AppContext.BaseDirectory, "Senparc.Xncf.Workspace.Agent.dll")
            : Path.GetFullPath(_options.AgentPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "Workspace Agent executable was not found. Configure SenparcXncfWorkspace:AgentPath.",
                path);
        }

        return path;
    }
}
