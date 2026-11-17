/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: ChatTaskCancellationRegistry.cs
    File description: Related implementation.

    Created by: Senparc - 20260927
    Change description: v0.18.1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

----------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Threading;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
///Legacy comment translated to English.
/// </summary>
public sealed class ChatTaskCancellationRegistry
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _sources = new();

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
    public void Unregister(int chatTaskId)
    {
        if (_sources.TryRemove(chatTaskId, out var source))
        {
            source.Dispose();
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public bool IsRegistered(int chatTaskId) => _sources.ContainsKey(chatTaskId);
}
