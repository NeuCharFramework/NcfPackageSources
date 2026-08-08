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
}

public sealed class SandboxCreateRuntimeResult
{
    public required string RuntimeHandle { get; init; }
    public int? HostPort { get; init; }
    public string? AccessUrl { get; init; }
    public string? AccessToken { get; init; }
    public string? Message { get; init; }
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

public sealed class SandboxQuotaPolicy
{
    public int MaxSessionsPerUser { get; init; } = 2;
    public int MaxGlobalSessions { get; init; } = 20;
    public double DefaultCpuLimit { get; init; } = 0.5;
    public int DefaultMemoryMb { get; init; } = 512;
    public TimeSpan DefaultTtl { get; init; } = TimeSpan.FromMinutes(45);
    public TimeSpan MaxTtl { get; init; } = TimeSpan.FromHours(4);
}
