/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxRuntimeModels.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260815
    Change description: v0.2.0 add the NCF preview sandbox workload

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

public sealed class SandboxTemplateDefinition
{
    /// <summary>Gets the stable template key.</summary>
    public required string Key { get; init; }
    /// <summary>Gets the display name shown to users.</summary>
    public required string DisplayName { get; init; }
    /// <summary>Gets the runtime preferred by the template.</summary>
    public required SandboxRuntimeKind PreferredRuntime { get; init; }
    /// <summary>Gets whether the template supports interactive sessions.</summary>
    public required bool Interactive { get; init; }
    /// <summary>Gets the container image used by the template.</summary>
    public required string Image { get; init; }
    /// <summary>Gets the default container port.</summary>
    public int ContainerPort { get; init; }
    /// <summary>Gets whether interactive control operations are supported.</summary>
    public bool SupportsInteractiveControl { get; init; }
    public string WorkspaceMountPath { get; init; } = "/workspace";
    public double DefaultCpuLimit { get; init; } = 0.5;
    public int DefaultMemoryMb { get; init; } = 512;
    public TimeSpan DefaultTtl { get; init; } = TimeSpan.FromMinutes(45);
}

public sealed class SandboxCreateRuntimeRequest
{
    public required string SessionId { get; init; }
    public required SandboxTemplateDefinition Template { get; init; }
    public required double CpuLimit { get; init; }
    public required int MemoryMb { get; init; }
    public required string WorkspaceDirectory { get; init; }
    /// <summary>
    /// Present only for the fixed-function NCF preview template. It is populated by the server,
    /// never by a browser or an AI-provided shell command.
    /// </summary>
    public SandboxNcfPreviewRuntimeOptions? NcfPreview { get; init; }
    /// <summary>Optional standard input sent to an interactive command.</summary>
    public IReadOnlyList<SandboxPortMapping>? ExtraPortMappings { get; init; }
}

public sealed class SandboxNcfPreviewRuntimeOptions
{
    public required string SolutionRelativePath { get; init; }
    public required string ModuleProjectName { get; init; }
    public required string BasePath { get; init; }
    public bool AllowDependencyRestoreNetwork { get; init; }
    public string? RestoreNetworkName { get; init; }
    public int StartupTimeoutSeconds { get; init; } = 180;
}

public sealed class SandboxCreateRuntimeResult
{
    public required string RuntimeHandle { get; init; }
    public int? HostPort { get; init; }
    public string? AccessUrl { get; init; }
    public string? AccessToken { get; init; }
    public string? Message { get; init; }
    /// <summary>Gets or sets additional host/container port mappings.</summary>
    public string? ExtraPorts { get; init; }
}

public sealed class SandboxExecRequest
{
    public required string SessionId { get; init; }
    public required SandboxTemplateDefinition Template { get; init; }
    public required string Code { get; init; }
    public required double CpuLimit { get; init; }
    public required int MemoryMb { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed class SandboxExecResult
{
    public required int ExitCode { get; init; }
    public required string StdOut { get; init; }
    public required string StdErr { get; init; }
}

public sealed class SandboxInteractiveExecRequest
{
    public required string SessionId { get; init; }
    public required string RuntimeHandle { get; init; }
    public required string Command { get; init; }
    public required string WorkingDirectory { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxOutputCharacters { get; init; } = 32_000;
    /// <summary>Gets or sets optional standard input for the interactive command.</summary>
    public string? StdinContent { get; init; }
}

/// <summary>
/// Describes a host/container port mapping and whether it is externally exposed.
/// </summary>
public sealed record SandboxPortMapping(int HostPort, int ContainerPort, bool ExposeExternally)
{
    /// <summary>
    /// Formats the mapping using loopback or all-interface host binding.
    /// </summary>
    /// <returns>A displayable host/container mapping string.</returns>
    public string ToDisplayString() =>
        $"{(ExposeExternally ? "0.0.0.0" : "127.0.0.1")}:{HostPort}:{ContainerPort}";
}

public sealed class SandboxWorkspaceFileInfo
{
    public required string RelativePath { get; init; }
    public long Length { get; init; }
    public DateTime LastWriteTimeUtc { get; init; }
}

public sealed class SandboxWorkspaceFileContent
{
    public required SandboxWorkspaceFileInfo File { get; init; }
    public required byte[] Content { get; init; }
}

public sealed class SandboxQuotaPolicy
{
    public int MaxSessionsPerUser { get; init; } = 10;
    public int MaxGlobalSessions { get; init; } = 50;
    public double DefaultCpuLimit { get; init; } = 0.5;
    public int DefaultMemoryMb { get; init; } = 512;
    public TimeSpan DefaultTtl { get; init; } = TimeSpan.FromMinutes(45);
    public TimeSpan MaxTtl { get; init; } = TimeSpan.FromHours(4);
    public int MaxInteractiveCommandSeconds { get; init; } = 120;
    public int MaxInteractiveCommandCharacters { get; init; } = 8_000;
    public int MaxInteractiveStdinCharacters { get; init; } = 32_000;
    public int MaxExtraPortMappings { get; init; } = 8;
    public int MaxInteractiveOutputCharacters { get; init; } = 32_000;
    public long MaxWorkspaceFileBytes { get; init; } = 3L * 1024 * 1024;
    public long MaxWorkspaceReadBytes { get; init; } = 2L * 1024 * 1024;
    public int MaxWorkspaceListItems { get; init; } = 200;
}
