/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatTaskCancellationRegistry.cs
    File description: Related implementation.
    Change description: v0.18.1 Updated module behavior and related support.

    Created by: Senparc - 20260927

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Threading;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Maintains linked cancellation sources for active chat tasks and disposes them when tasks finish.
/// </summary>
public sealed class ChatTaskCancellationRegistry
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _sources = new();

    /// <summary>
    /// Registers or replaces the cancellation source for a chat task.
    /// </summary>
    /// <param name="chatTaskId">The chat task identifier.</param>
    /// <param name="externalToken">An optional caller token linked to the task token.</param>
    /// <returns>The token that should be observed by the task execution.</returns>
    public CancellationToken Register(int chatTaskId, CancellationToken externalToken)
    {
        CancellationTokenSource source = externalToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(externalToken)
            : new CancellationTokenSource();

        _sources.AddOrUpdate(chatTaskId, source, (_, existing) =>
        {
            existing.Cancel();
            existing.Dispose();
            return source;
        });

        return source.Token;
    }

    /// <summary>
    /// Requests cancellation for a registered chat task.
    /// </summary>
    /// <param name="chatTaskId">The chat task identifier to cancel.</param>
    /// <returns><see langword="true"/> when the task was registered; otherwise, <see langword="false"/>.</returns>
    public bool TryCancel(int chatTaskId)
    {
        if (_sources.TryGetValue(chatTaskId, out var source) && !source.IsCancellationRequested)
        {
            source.Cancel();
            return true;
        }

        return _sources.ContainsKey(chatTaskId);
    }

    /// <summary>
    /// Removes and disposes the cancellation source for a chat task.
    /// </summary>
    /// <param name="chatTaskId">The chat task identifier to unregister.</param>
    public void Unregister(int chatTaskId)
    {
        if (_sources.TryRemove(chatTaskId, out var source))
        {
            source.Dispose();
        }
    }

    /// <summary>
    /// Determines whether a chat task currently has a registered cancellation source.
    /// </summary>
    /// <param name="chatTaskId">The chat task identifier to inspect.</param>
    /// <returns><see langword="true"/> when a source is registered.</returns>
    public bool IsRegistered(int chatTaskId) => _sources.ContainsKey(chatTaskId);
}
