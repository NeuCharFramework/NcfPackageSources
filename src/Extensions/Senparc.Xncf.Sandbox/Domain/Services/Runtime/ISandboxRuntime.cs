/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: ISandboxRuntime.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20260808
    
    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

----------------------------------------------------------------*/
using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
///Legacy comment translated to English.
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
