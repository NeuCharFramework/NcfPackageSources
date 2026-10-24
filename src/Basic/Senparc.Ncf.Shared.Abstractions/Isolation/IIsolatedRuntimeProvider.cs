/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IIsolatedRuntimeProvider.cs
    文件功能描述：隔离运行时公共契约

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Isolation;

public interface IIsolatedRuntimeProvider
{
    string ProviderName { get; }

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<IsolatedRuntimeCommandResult> RunAsync(
        IsolatedRuntimeSpec specification,
        CancellationToken cancellationToken = default);

    IIsolatedRuntimeConnection StartAttached(IsolatedRuntimeSpec specification);

    Task<IsolatedRuntimeCommandResult> ExecAsync(
        IsolatedRuntimeExecRequest request,
        CancellationToken cancellationToken = default);

    Task<string> GetLogsAsync(
        string runtimeHandle,
        int tailLines,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string runtimeHandle,
        bool force = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListHandlesAsync(
        IsolatedRuntimeListRequest request,
        CancellationToken cancellationToken = default);
}

public interface IIsolatedRuntimeConnection : IAsyncDisposable
{
    int ProcessId { get; }

    bool HasExited { get; }

    int ExitCode { get; }

    StreamWriter StandardInput { get; }

    StreamReader StandardOutput { get; }

    StreamReader StandardError { get; }

    Task WaitForExitAsync(CancellationToken cancellationToken = default);

    void Terminate();
}

public sealed record IsolatedRuntimeMount(
    string Source,
    string Target,
    bool ReadOnly = false);

public sealed record IsolatedRuntimePortBinding(
    string HostAddress,
    int HostPort,
    int ContainerPort);

public sealed record IsolatedRuntimeTmpfsMount(
    string Target,
    string Options);

public sealed class IsolatedRuntimeSpec
{
    public string Image { get; init; } = string.Empty;

    public IReadOnlyList<string> Command { get; init; } = Array.Empty<string>();

    public string Name { get; init; } = string.Empty;

    public bool Detached { get; init; }

    public bool AutoRemove { get; init; }

    public bool OpenStandardInput { get; init; }

    public bool ReadOnlyRootFileSystem { get; init; } = true;

    public bool DropAllCapabilities { get; init; } = true;

    public bool NoNewPrivileges { get; init; } = true;

    public string NetworkMode { get; init; } = "none";

    public string WorkingDirectory { get; init; } = string.Empty;

    public string User { get; init; } = string.Empty;

    public double CpuLimit { get; init; } = 1;

    public int MemoryMegabytes { get; init; } = 512;

    public int PidsLimit { get; init; } = 128;

    public IReadOnlyDictionary<string, string> Labels { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Environment { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<IsolatedRuntimeMount> Mounts { get; init; } =
        Array.Empty<IsolatedRuntimeMount>();

    public IReadOnlyList<IsolatedRuntimePortBinding> Ports { get; init; } =
        Array.Empty<IsolatedRuntimePortBinding>();

    public IReadOnlyList<IsolatedRuntimeTmpfsMount> TmpfsMounts { get; init; } =
        Array.Empty<IsolatedRuntimeTmpfsMount>();

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);

    public int MaxOutputCharacters { get; init; }

    public string? StandardInput { get; init; }
}

public sealed record IsolatedRuntimeExecRequest(
    string RuntimeHandle,
    IReadOnlyList<string> Command,
    string WorkingDirectory = "",
    string? StandardInput = null,
    TimeSpan? Timeout = null,
    int MaxOutputCharacters = 0,
    IReadOnlyDictionary<string, string>? Environment = null);

public sealed record IsolatedRuntimeListRequest(
    IReadOnlyDictionary<string, string> Labels,
    bool IncludeStopped = false);

public sealed record IsolatedRuntimeCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
