/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WasmSandboxRuntime.cs
    文件功能描述：Wasm 轻量沙箱运行时（一期 Stub）

    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// Wasm 后端占位。二期可接入 Wasmtime / WASI / .NET WASI SDK，用于短任务评测以降压。
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
