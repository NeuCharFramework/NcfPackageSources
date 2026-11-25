/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceAgentProcessFactory.cs
    File description: create native or Docker Workspace Agent processes across platforms

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.2 enhance workspace functionality with file preview and management features

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

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

    /// <summary>
    /// Initializes native/Docker Workspace Agent process creation from workspace options.
    /// </summary>
    /// <param name="options">Workspace runtime and isolation options.</param>
    /// <param name="logger">Logger for process lifecycle failures.</param>
    /// <param name="isolatedRuntimeProviders">Optional isolated runtime providers used for Docker mode.</param>
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

    /// <summary>
    /// Starts a Workspace Agent using the configured native or Docker provider.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier used for labels and diagnostics.</param>
    /// <param name="workspaceRoot">The physical workspace root exposed to the agent.</param>
    /// <returns>An active isolated-runtime connection.</returns>
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

    /// <summary>
    /// Starts the configured native Workspace Agent process.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier used for diagnostics.</param>
    /// <param name="workspaceRoot">The physical workspace root.</param>
    /// <returns>An active process connection.</returns>
    private IIsolatedRuntimeConnection StartNative(string workspaceId, string workspaceRoot)
    {
        var agentPath = ResolveAgentPath();
        var startInfo = CreateBaseStartInfo(
            string.Equals(Path.GetExtension(agentPath), ".dll", StringComparison.OrdinalIgnoreCase)
                ? "dotnet"
                : agentPath);
        if (string.Equals(Path.GetExtension(agentPath), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add(agentPath);
        }

        AddAgentArguments(
            startInfo.ArgumentList,
            workspaceRoot,
            _options.AllowNativeExecution,
            "NativeFileAccess");
        return StartProcess(workspaceId, startInfo);
    }

    /// <summary>
    /// Starts a Docker Workspace Agent with a read-only root, no network, and a workspace bind mount.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier used for container naming and labels.</param>
    /// <param name="workspaceRoot">The physical workspace root mounted at <c>/workspace</c>.</param>
    /// <returns>An active attached container connection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Docker configuration or provider registration is missing.</exception>
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

    /// <summary>
    /// Creates a non-shell process configuration with redirected standard streams.
    /// </summary>
    /// <param name="fileName">The executable or host process name.</param>
    /// <returns>A process-start configuration for the Workspace Agent.</returns>
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

    /// <summary>
    /// Appends the configured Workspace Agent limits and execution policy to an argument list.
    /// </summary>
    /// <param name="arguments">The mutable argument list.</param>
    /// <param name="rootPath">The agent-visible workspace root.</param>
    /// <param name="executionEnabled">Whether command execution is enabled.</param>
    /// <param name="isolationLevel">The diagnostic isolation-level label.</param>
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

    /// <summary>
    /// Starts a native process and logs failures with workspace/provider context.
    /// </summary>
    /// <param name="workspaceId">The workspace identifier.</param>
    /// <param name="startInfo">The process-start configuration.</param>
    /// <returns>An active process connection.</returns>
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

    /// <summary>
    /// Resolves the configured agent path or the standard output-directory locations.
    /// </summary>
    /// <returns>An existing agent DLL or executable path.</returns>
    /// <exception cref="FileNotFoundException">Thrown when no agent path can be resolved.</exception>
    private string ResolveAgentPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.AgentPath))
        {
            var configuredPath = Path.GetFullPath(_options.AgentPath);
            if (!File.Exists(configuredPath))
            {
                throw new FileNotFoundException(
                    "Workspace Agent executable was not found. Configure SenparcXncfWorkspace:AgentPath.",
                    configuredPath);
            }

            return configuredPath;
        }

        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "Senparc.Xncf.WorkspaceAgent.dll");
        if (File.Exists(assemblyPath))
        {
            return assemblyPath;
        }

        var executablePath = Path.Combine(AppContext.BaseDirectory, "Senparc.Xncf.WorkspaceAgent");
        if (File.Exists(executablePath))
        {
            return executablePath;
        }

        throw new FileNotFoundException(
            "Workspace Agent executable was not found. Configure SenparcXncfWorkspace:AgentPath.",
            assemblyPath);
    }
}
