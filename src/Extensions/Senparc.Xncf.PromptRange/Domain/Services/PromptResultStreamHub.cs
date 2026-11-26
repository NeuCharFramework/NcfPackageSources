/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultStreamHub.cs
    File description: Related implementation.

    Created by: Senparc - 20260626

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public sealed class PromptResultStreamHub
{
    private sealed class StreamGroup
    {
        public readonly object Sync = new();
        public readonly List<PromptResultStreamEvent> Buffer = new();
        public readonly ConcurrentDictionary<Guid, Channel<PromptResultStreamEvent>> Subscribers = new();
        public bool IsComplete;
    }

    private readonly ConcurrentDictionary<string, StreamGroup> _streams = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Subscribes to one result stream and replays buffered events before live events.
    /// </summary>
    /// <param name="streamId">The result stream identifier.</param>
    /// <param name="cancellationToken">Token used to terminate the subscription.</param>
    /// <returns>An asynchronous sequence of stream events.</returns>
    public async IAsyncEnumerable<PromptResultStreamEvent> Subscribe(
        string streamId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            yield break;
        }

        var group = _streams.GetOrAdd(streamId, _ => new StreamGroup());
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<PromptResultStreamEvent>();
        group.Subscribers[subscriptionId] = channel;

        List<PromptResultStreamEvent> bufferedEvents;
        lock (group.Sync)
        {
            bufferedEvents = new List<PromptResultStreamEvent>(group.Buffer);
        }

        foreach (var bufferedEvent in bufferedEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return bufferedEvent;
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            group.Subscribers.TryRemove(subscriptionId, out _);
            CleanupStreamIfFinished(streamId, group);
        }
    }

    /// <summary>
    /// Buffers and broadcasts one prompt result stream event.
    /// </summary>
    /// <param name="streamEvent">The event to publish.</param>
    public void Publish(PromptResultStreamEvent streamEvent)
    {
        if (streamEvent == null || string.IsNullOrWhiteSpace(streamEvent.StreamId))
        {
            return;
        }

        var group = _streams.GetOrAdd(streamEvent.StreamId, _ => new StreamGroup());
        lock (group.Sync)
        {
            group.Buffer.Add(streamEvent);
            if (string.Equals(streamEvent.EventType, "complete", StringComparison.OrdinalIgnoreCase))
            {
                group.IsComplete = true;
            }
        }

        if (group.Subscribers.IsEmpty)
        {
            return;
        }

        foreach (var pair in group.Subscribers)
        {
            if (!pair.Value.Writer.TryWrite(streamEvent))
            {
                pair.Value.Writer.TryComplete();
                group.Subscribers.TryRemove(pair.Key, out _);
            }
        }

        if (group.IsComplete)
        {
            CleanupStreamIfFinished(streamEvent.StreamId, group);
        }
    }

    /// <summary>
    /// Removes a completed stream when it no longer has subscribers.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="group">The stream group.</param>
    private void CleanupStreamIfFinished(string streamId, StreamGroup group)
    {
        if (!group.IsComplete || !group.Subscribers.IsEmpty)
        {
            return;
        }

        _streams.TryRemove(streamId, out _);
    }
}

public sealed class PromptResultStreamEvent
{
    public string StreamId { get; set; }
    public string EventType { get; set; } = "chunk";
    public int? PromptResultId { get; set; }
    public string Text { get; set; }
    public bool IsFinal { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public int ResponseMilliseconds { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}
