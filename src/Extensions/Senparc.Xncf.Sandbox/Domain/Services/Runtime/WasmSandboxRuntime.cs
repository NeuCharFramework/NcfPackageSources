/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WasmSandboxRuntime.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed class WasmSandboxRuntime : ISandboxRuntime
{
    private readonly ILogger<WasmSandboxRuntime> _logger;

    public WasmSandboxRuntime(ILogger<WasmSandboxRuntime> logger)
    {
        _logger = logger;
    }

    public SandboxRuntimeKind Kind => SandboxRuntimeKind.Wasm;

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Wasm sandbox runtime is stubbed and currently unavailable.");
        return Task.FromResult(false);
    }

    public Task<SandboxCreateRuntimeResult> CreateInteractiveAsync(
        SandboxCreateRuntimeRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Wasm 运行时尚未实现交互式会话。请使用 Docker + Jupyter 模板，或等待 Wasm Provider 落地。");
    }

    public Task<SandboxExecResult> ExecAsync(
        SandboxExecRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Wasm Exec Provider 为一期 Stub。计划二期接入 Wasmtime 以降低服务器压力。");
    }

    public Task<SandboxExecResult> ExecInteractiveAsync(
        SandboxInteractiveExecRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Wasm 运行时尚未实现持久化交互会话控制。");
    }

    public Task DestroyAsync(string runtimeHandle, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListOrphanHandlesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    public Task<IReadOnlyList<string>> ListRunningHandlesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
