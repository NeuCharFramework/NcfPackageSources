using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// 可替换的沙箱运行时后端。
/// </summary>
public interface ISandboxRuntime
{
    SandboxRuntimeKind Kind { get; }

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<SandboxCreateRuntimeResult> CreateInteractiveAsync(
        SandboxCreateRuntimeRequest request,
        CancellationToken cancellationToken = default);

    Task<SandboxExecResult> ExecAsync(
        SandboxExecRequest request,
        CancellationToken cancellationToken = default);

    Task DestroyAsync(string runtimeHandle, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListOrphanHandlesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListRunningHandlesAsync(CancellationToken cancellationToken = default);
}
