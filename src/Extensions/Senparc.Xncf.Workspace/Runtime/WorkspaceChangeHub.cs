/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceChangeHub.cs
    文件功能描述：Workspace 文件变更通知中心

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceChangeHub
{
    private const int ReplayCapacity = 200;
    private const int SubscriberCapacity = 256;
    private readonly object _gate = new();
    private readonly Queue<WorkspaceChangeEvent> _replay = new();
    private readonly Dictionary<Guid, Subscriber> _subscribers = new();
    private long _sequence;

    public WorkspaceChangeEvent Publish(
        string workspaceId,
        string relativePath,
        string changeType,
        string previousRelativePath = "",
        string etag = "")
    {
        var message = new WorkspaceChangeEvent(
            Interlocked.Increment(ref _sequence),
            workspaceId,
            relativePath,
            changeType,
            DateTimeOffset.UtcNow,
            previousRelativePath,
            etag);
        Subscriber[] subscribers;
        lock (_gate)
        {
            _replay.Enqueue(message);
            while (_replay.Count > ReplayCapacity)
            {
                _replay.Dequeue();
            }

            subscribers = _subscribers.Values.ToArray();
        }

        foreach (var subscriber in subscribers)
        {
            if (string.Equals(subscriber.WorkspaceId, workspaceId, StringComparison.Ordinal))
            {
                subscriber.Channel.Writer.TryWrite(message);
            }
        }

        return message;
    }

    public async IAsyncEnumerable<WorkspaceChangeEvent> Subscribe(
        string workspaceId,
        bool replayBuffered,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<WorkspaceChangeEvent>(new BoundedChannelOptions(SubscriberCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
        WorkspaceChangeEvent[] buffered;
        lock (_gate)
        {
            buffered = replayBuffered
                ? _replay.Where(message =>
                    string.Equals(message.WorkspaceId, workspaceId, StringComparison.Ordinal)).ToArray()
                : [];
            _subscribers[id] = new Subscriber(workspaceId, channel);
        }

        try
        {
            foreach (var message in buffered)
            {
                yield return message;
            }

            await foreach (var message in channel.Reader
                               .ReadAllAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return message;
            }
        }
        finally
        {
            lock (_gate)
            {
                _subscribers.Remove(id);
            }

            channel.Writer.TryComplete();
        }
    }

    private sealed record Subscriber(
        string WorkspaceId,
        Channel<WorkspaceChangeEvent> Channel);
}
