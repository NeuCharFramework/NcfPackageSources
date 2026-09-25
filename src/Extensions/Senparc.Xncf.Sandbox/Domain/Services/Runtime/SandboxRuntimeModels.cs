/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxRuntimeModels.cs
    文件功能描述：沙箱模板、运行时请求和配额模型


    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱工作负载

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

    修改标识：Senparc - 20260918
    修改描述：v0.3.3 提高会话配额并增加附加端口映射与交互式标准输入

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

public sealed class SandboxTemplateDefinition
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required SandboxRuntimeKind PreferredRuntime { get; init; }
    public required bool Interactive { get; init; }
    public required string Image { get; init; }
    public int ContainerPort { get; init; }
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
    /// <summary>
    /// 可选的附加端口映射（仅 Jupyter 交互式模板）。HostPort 为 0 表示由运行时自动分配。
    /// </summary>
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
    /// <summary>
    /// 解析完成后的附加端口映射（用于持久化展示）；无则为空。
    /// </summary>
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
    /// <summary>
    /// 可选的标准输入内容；提供时通过 docker exec -i 写入并关闭 stdin，
    /// 用于向交互式程序（REPL/终端程序）批量提交指令。
    /// </summary>
    public string? StdinContent { get; init; }
}

/// <summary>
/// 单条附加端口映射。HostPort 为 0 表示运行时自动分配空闲端口。
/// </summary>
public sealed record SandboxPortMapping(int HostPort, int ContainerPort, bool ExposeExternally)
{
    /// <summary>
    /// 持久化/展示格式：loopback 为 127.0.0.1:host:container，外部为 0.0.0.0:host:container。
    /// </summary>
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
