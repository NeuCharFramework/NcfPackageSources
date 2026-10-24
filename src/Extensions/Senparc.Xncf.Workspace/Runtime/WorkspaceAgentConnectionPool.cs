/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceAgentConnectionPool.cs
    文件功能描述：按 Workspace 复用 Agent 长连接

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceAgentConnectionPool : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<WorkspaceAgentConnection>> _connections =
        new(StringComparer.Ordinal);
    private readonly WorkspaceAgentProcessFactory _processFactory;
    private readonly ILogger<WorkspaceAgentConnectionPool> _logger;

    public WorkspaceAgentConnectionPool(
        WorkspaceAgentProcessFactory processFactory,
        ILogger<WorkspaceAgentConnectionPool> logger)
    {
        _processFactory = processFactory;
        _logger = logger;
    }

    public Task<TResult> InvokeAsync<TPayload, TResult>(
        string workspaceId,
        string workspaceRoot,
        string operation,
        TPayload payload,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection(workspaceId, workspaceRoot);
        return InvokeWithRecoveryAsync<TPayload, TResult>(
            connection,
            workspaceId,
            workspaceRoot,
            operation,
            payload,
            cancellationToken);
    }

    public async Task StopAsync(string workspaceId)
    {
        if (_connections.TryRemove(workspaceId, out var connection) && connection.IsValueCreated)
        {
            await connection.Value.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        var connections = _connections.ToArray();
        _connections.Clear();
        foreach (var connection in connections)
        {
            if (connection.Value.IsValueCreated)
            {
                await connection.Value.Value.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private WorkspaceAgentConnection GetConnection(string workspaceId, string workspaceRoot)
    {
        return _connections.GetOrAdd(
            workspaceId,
            _ => new Lazy<WorkspaceAgentConnection>(
                () => new WorkspaceAgentConnection(
                    _processFactory.Start(workspaceId, workspaceRoot),
                    workspaceId,
                    _logger),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    private async Task<TResult> InvokeWithRecoveryAsync<TPayload, TResult>(
        WorkspaceAgentConnection connection,
        string workspaceId,
        string workspaceRoot,
        string operation,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            return await connection
                .InvokeAsync<TPayload, TResult>(operation, payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (IOException) when (connection.HasExited)
        {
            await StopAsync(workspaceId).ConfigureAwait(false);
            return await GetConnection(workspaceId, workspaceRoot)
                .InvokeAsync<TPayload, TResult>(operation, payload, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
