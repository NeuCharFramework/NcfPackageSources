/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IIsolatedRuntimeProvider.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.7.0-preview1 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.7.1-preview2 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.7.1.2-preview3 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Isolation;

/// <summary>
/// Defines the lifecycle and command contract for a process-isolated runtime provider.
/// Implementations may be backed by Docker, a local process, or another isolated execution host.
/// </summary>
public interface IIsolatedRuntimeProvider
{
    /// <summary>Gets the stable provider name used in diagnostics and selection.</summary>
    string ProviderName { get; }

    /// <summary>Determines whether the provider is available on the current host.</summary>
    /// <param name="cancellationToken">Token used to cancel the availability probe.</param>
    /// <returns><see langword="true"/> when the provider can accept runtime operations.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs a configured isolated command and captures bounded output.</summary>
    /// <param name="specification">The image, command, resource limits, mounts, and environment configuration.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The exit code and captured standard output/error.</returns>
    Task<IsolatedRuntimeCommandResult> RunAsync(
        IsolatedRuntimeSpec specification,
        CancellationToken cancellationToken = default);

    /// <summary>Starts a long-running isolated process with attached standard streams.</summary>
    /// <param name="specification">The runtime configuration for the attached process.</param>
    /// <returns>An attached connection that owns the process streams and lifecycle.</returns>
    IIsolatedRuntimeConnection StartAttached(IsolatedRuntimeSpec specification);

    /// <summary>Executes a command inside an existing isolated runtime.</summary>
    /// <param name="request">The runtime handle, command, working directory, input, and output limits.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The exit code and captured standard output/error.</returns>
    Task<IsolatedRuntimeCommandResult> ExecAsync(
        IsolatedRuntimeExecRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the most recent log lines for an isolated runtime.</summary>
    /// <param name="runtimeHandle">The provider-specific runtime handle.</param>
    /// <param name="tailLines">The maximum number of trailing lines requested.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The selected log text.</returns>
    Task<string> GetLogsAsync(
        string runtimeHandle,
        int tailLines,
        CancellationToken cancellationToken = default);

    /// <summary>Removes an isolated runtime and optionally forces termination.</summary>
    /// <param name="runtimeHandle">The provider-specific runtime handle.</param>
    /// <param name="force">Whether the provider should force removal of a running runtime.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RemoveAsync(
        string runtimeHandle,
        bool force = true,
        CancellationToken cancellationToken = default);

    /// <summary>Lists runtime handles matching the supplied labels and stopped-state filter.</summary>
    /// <param name="request">The label selector and stopped-runtime inclusion flag.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching provider-specific runtime handles.</returns>
    Task<IReadOnlyList<string>> ListHandlesAsync(
        IsolatedRuntimeListRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an attached isolated process and its asynchronous standard streams.
/// </summary>
public interface IIsolatedRuntimeConnection : IAsyncDisposable
{
    int ProcessId { get; }

    bool HasExited { get; }

    int ExitCode { get; }

    StreamWriter StandardInput { get; }

    StreamReader StandardOutput { get; }

    StreamReader StandardError { get; }

    /// <summary>Waits until the attached process exits.</summary>
    /// <param name="cancellationToken">Token used to stop waiting.</param>
    Task WaitForExitAsync(CancellationToken cancellationToken = default);

    /// <summary>Requests termination of the attached process.</summary>
    void Terminate();
}

/// <summary>
/// Describes one host-to-runtime filesystem mount.
/// </summary>
public sealed record IsolatedRuntimeMount(
    string Source,
    string Target,
    bool ReadOnly = false);

/// <summary>
/// Describes one host-to-runtime port binding.
/// </summary>
public sealed record IsolatedRuntimePortBinding(
    string HostAddress,
    int HostPort,
    int ContainerPort);

/// <summary>
/// Describes one temporary filesystem mount inside an isolated runtime.
/// </summary>
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

/// <summary>
/// Describes a command execution request for an existing isolated runtime.
/// </summary>
public sealed record IsolatedRuntimeExecRequest(
    string RuntimeHandle,
    IReadOnlyList<string> Command,
    string WorkingDirectory = "",
    string? StandardInput = null,
    TimeSpan? Timeout = null,
    int MaxOutputCharacters = 0,
    IReadOnlyDictionary<string, string>? Environment = null);

/// <summary>
/// Describes a query for isolated runtimes matching labels.
/// </summary>
public sealed record IsolatedRuntimeListRequest(
    IReadOnlyDictionary<string, string> Labels,
    bool IncludeStopped = false);

/// <summary>
/// Contains exit code and captured output from an isolated-runtime command.
/// </summary>
public sealed record IsolatedRuntimeCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
