/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResultStreamHub.cs
    文件功能描述：PromptResultStreamHub 服务逻辑
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

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
