/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceAgentConnection.cs
    文件功能描述：Workspace Agent 持久 stdio 协议连接

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Shared.Abstractions.Isolation;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace;

internal sealed class WorkspaceAgentConnection : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IIsolatedRuntimeConnection _process;
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private readonly ILogger _logger;
    private readonly string _workspaceId;
    private readonly Task _errorPump;
    private bool _disposed;

    public WorkspaceAgentConnection(
        IIsolatedRuntimeConnection process,
        string workspaceId,
        ILogger logger)
    {
        _process = process;
        _workspaceId = workspaceId;
        _logger = logger;
        _errorPump = PumpErrorsAsync();
    }

    public bool HasExited => _process.HasExited;

    public async Task<TResult> InvokeAsync<TPayload, TResult>(
        string operation,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _requestLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_process.HasExited)
            {
                throw new IOException(
                    $"Workspace Agent exited unexpectedly with code {_process.ExitCode}.");
            }

            var request = new WorkspaceAgentRequest(
                Guid.NewGuid().ToString("N"),
                operation,
                JsonSerializer.SerializeToElement(payload, JsonOptions));
            await _process.StandardInput
                .WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions).AsMemory(), cancellationToken)
                .ConfigureAwait(false);
            await _process.StandardInput.FlushAsync().ConfigureAwait(false);
            var line = await _process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                throw new IOException("Workspace Agent closed its response stream.");
            }

            var response = JsonSerializer.Deserialize<WorkspaceAgentResponse>(line, JsonOptions)
                           ?? throw new IOException("Workspace Agent returned an empty response.");
            if (!string.Equals(response.Id, request.Id, StringComparison.Ordinal))
            {
                throw new IOException("Workspace Agent returned a mismatched response.");
            }

            if (!response.Succeeded)
            {
                ThrowAgentError(response.Error);
            }

            if (typeof(TResult) == typeof(WorkspaceAgentEmptyResult))
            {
                return (TResult)(object)WorkspaceAgentEmptyResult.Value;
            }

            if (response.Result is not { } resultElement)
            {
                throw new IOException("Workspace Agent returned an invalid result.");
            }

            var result = resultElement.Deserialize<TResult>(JsonOptions);
            if (result is null)
            {
                throw new IOException("Workspace Agent returned an invalid result.");
            }

            return result;
        }
        finally
        {
            _requestLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            if (!_process.HasExited)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await InvokeShutdownAsync(timeout.Token).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is IOException or OperationCanceledException)
        {
            _logger.LogDebug(exception, "Workspace Agent graceful shutdown failed for {WorkspaceId}", _workspaceId);
        }

        if (!_process.HasExited)
        {
            _process.Terminate();
        }

        await _errorPump.ConfigureAwait(false);
        _requestLock.Dispose();
        await _process.DisposeAsync().ConfigureAwait(false);
    }

    private async Task InvokeShutdownAsync(CancellationToken cancellationToken)
    {
        var request = new WorkspaceAgentRequest(
            Guid.NewGuid().ToString("N"),
            WorkspaceAgentOperations.Shutdown,
            JsonSerializer.SerializeToElement(new { }, JsonOptions));
        await _process.StandardInput
            .WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions).AsMemory(), cancellationToken)
            .ConfigureAwait(false);
        await _process.StandardInput.FlushAsync().ConfigureAwait(false);
        _ = await _process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PumpErrorsAsync()
    {
        while (await _process.StandardError.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                _logger.LogWarning(
                    "Workspace Agent diagnostic: {WorkspaceId}, {Diagnostic}",
                    _workspaceId,
                    line);
            }
        }
    }

    private static void ThrowAgentError(WorkspaceAgentError? error)
    {
        var message = error?.Message ?? "Workspace Agent operation failed.";
        throw error?.Code switch
        {
            "conflict" => new WorkspaceConflictException(message),
            "forbidden" => new UnauthorizedAccessException(message),
            "not-found" => new FileNotFoundException(message),
            "invalid-request" => new ArgumentException(message),
            "io-error" => new IOException(message),
            "cancelled" => new OperationCanceledException(message),
            _ => new InvalidOperationException(message)
        };
    }
}

internal sealed class WorkspaceAgentEmptyResult
{
    public static WorkspaceAgentEmptyResult Value { get; } = new();
}
