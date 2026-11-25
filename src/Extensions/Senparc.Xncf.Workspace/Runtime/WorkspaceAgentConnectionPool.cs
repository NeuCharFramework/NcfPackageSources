/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceAgentConnectionPool.cs
    File description: reuse long-lived Agent connections by Workspace

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.2 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

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

    /// <summary>
    /// Initializes a per-workspace Workspace Agent connection pool.
    /// </summary>
    /// <param name="processFactory">Factory used to start agent processes.</param>
    /// <param name="logger">Logger shared by pooled connections.</param>
    public WorkspaceAgentConnectionPool(
        WorkspaceAgentProcessFactory processFactory,
        ILogger<WorkspaceAgentConnectionPool> logger)
    {
        _processFactory = processFactory;
        _logger = logger;
    }

    /// <summary>
    /// Invokes an operation through the workspace's reusable connection, recovering once from process exit.
    /// </summary>
    /// <typeparam name="TPayload">The request payload type.</typeparam>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="workspaceId">The workspace identifier used as the pool key.</param>
    /// <param name="workspaceRoot">The physical workspace root passed to a new connection.</param>
    /// <param name="operation">The agent operation name.</param>
    /// <param name="payload">The operation payload.</param>
    /// <param name="cancellationToken">Token used to cancel the invocation.</param>
    /// <returns>A task producing the operation result.</returns>
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

    /// <summary>
    /// Stops and removes one workspace's pooled connection.
    /// </summary>
    /// <param name="workspaceId">The workspace pool key.</param>
    /// <returns>A task that completes after connection disposal.</returns>
    public async Task StopAsync(string workspaceId)
    {
        if (_connections.TryRemove(workspaceId, out var connection) && connection.IsValueCreated)
        {
            await connection.Value.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disposes every created workspace connection in the pool.
    /// </summary>
    /// <returns>A value task that completes after all connections are disposed.</returns>
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

    /// <summary>
    /// Gets or lazily creates the connection associated with a workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace pool key.</param>
    /// <param name="workspaceRoot">The physical workspace root for first creation.</param>
    /// <returns>The pooled connection.</returns>
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

    /// <summary>
    /// Invokes an operation and retries once after removing an exited agent connection.
    /// </summary>
    /// <typeparam name="TPayload">The request payload type.</typeparam>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="connection">The current pooled connection.</param>
    /// <param name="workspaceId">The workspace pool key.</param>
    /// <param name="workspaceRoot">The physical workspace root used for recovery.</param>
    /// <param name="operation">The agent operation name.</param>
    /// <param name="payload">The operation payload.</param>
    /// <param name="cancellationToken">Token used to cancel invocation.</param>
    /// <returns>The operation result.</returns>
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
