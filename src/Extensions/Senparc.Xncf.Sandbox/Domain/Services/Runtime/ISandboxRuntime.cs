/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ISandboxRuntime.cs
    文件功能描述：ISandboxRuntime.cs 功能实现
    
    
    创建标识：Senparc - 20260808
    
    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理
----------------------------------------------------------------*/
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

    Task<SandboxExecResult> ExecInteractiveAsync(
        SandboxInteractiveExecRequest request,
        CancellationToken cancellationToken = default);

    Task DestroyAsync(string runtimeHandle, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListOrphanHandlesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListRunningHandlesAsync(CancellationToken cancellationToken = default);
}
