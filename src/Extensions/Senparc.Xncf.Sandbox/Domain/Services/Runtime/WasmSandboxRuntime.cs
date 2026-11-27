/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WasmSandboxRuntime.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

/// <summary>
/// Placeholder <see cref="ISandboxRuntime"/> implementation for a future WebAssembly provider.
/// Availability is reported as false and execution methods fail explicitly until a Wasmtime-backed
/// implementation is supplied.
/// </summary>
public sealed class WasmSandboxRuntime : ISandboxRuntime
{
    private readonly ILogger<WasmSandboxRuntime> _logger;

    /// <summary>
    /// Initializes the placeholder WebAssembly runtime.
    /// </summary>
    /// <param name="logger">The logger used to record unavailable-runtime diagnostics.</param>
    public WasmSandboxRuntime(ILogger<WasmSandboxRuntime> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public SandboxRuntimeKind Kind => SandboxRuntimeKind.Wasm;

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Wasm sandbox runtime is stubbed and currently unavailable.");
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown because the WebAssembly provider is not implemented.</exception>
    public Task<SandboxCreateRuntimeResult> CreateInteractiveAsync(
        SandboxCreateRuntimeRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Wasm 运行时尚未实现交互式会话。请使用 Docker + Jupyter 模板，或等待 Wasm Provider 落地。");
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown because the WebAssembly provider is not implemented.</exception>
    public Task<SandboxExecResult> ExecAsync(
        SandboxExecRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Wasm Exec Provider 为一期 Stub。计划二期接入 Wasmtime 以降低服务器压力。");
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown because persistent WebAssembly sessions are not implemented.</exception>
    public Task<SandboxExecResult> ExecInteractiveAsync(
        SandboxInteractiveExecRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Wasm 运行时尚未实现持久化交互会话控制。");
    }

    /// <inheritdoc />
    public Task DestroyAsync(string runtimeHandle, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListOrphanHandlesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListRunningHandlesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
