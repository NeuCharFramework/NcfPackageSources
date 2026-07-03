/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatTaskStreamHub.cs
    文件功能描述：ChatTaskStreamHub 服务逻辑
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

public sealed class ChatTaskStreamHub
{
    private sealed class StreamGroup
    {
        public readonly object Sync = new();
        public readonly List<ChatTaskStreamEvent> Buffer = new();
        public readonly ConcurrentDictionary<Guid, Channel<ChatTaskStreamEvent>> Subscribers = new();
        public bool IsComplete;
    }

    private readonly ConcurrentDictionary<int, StreamGroup> _streams = new();

    public async IAsyncEnumerable<ChatTaskStreamEvent> Subscribe(
        int chatTaskId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (chatTaskId <= 0)
        {
            yield break;
        }

        var group = _streams.GetOrAdd(chatTaskId, _ => new StreamGroup());
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<ChatTaskStreamEvent>();
        group.Subscribers[subscriptionId] = channel;

        List<ChatTaskStreamEvent> bufferedEvents;
        lock (group.Sync)
        {
            bufferedEvents = new List<ChatTaskStreamEvent>(group.Buffer);
        }

        DateTimeOffset? previousBufferedTimestamp = null;
        foreach (var bufferedEvent in bufferedEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var replayDelayMs = GetBufferedReplayDelayMilliseconds(bufferedEvent, previousBufferedTimestamp);
            if (replayDelayMs > 0)
            {
                await Task.Delay(replayDelayMs, cancellationToken);
            }

            previousBufferedTimestamp = bufferedEvent.Timestamp;
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
            CleanupStreamIfFinished(chatTaskId, group);
        }
    }

    public void Publish(ChatTaskStreamEvent item)
    {
        if (item == null)
        {
            return;
        }

        var group = _streams.GetOrAdd(item.ChatTaskId, _ => new StreamGroup());
        lock (group.Sync)
        {
            group.Buffer.Add(item);
            if (IsTerminalStatusEvent(item))
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
            if (!pair.Value.Writer.TryWrite(item))
            {
                pair.Value.Writer.TryComplete();
                group.Subscribers.TryRemove(pair.Key, out _);
            }
        }

        if (group.IsComplete)
        {
            CleanupStreamIfFinished(item.ChatTaskId, group);
        }
    }

    private static bool IsTerminalStatusEvent(ChatTaskStreamEvent item)
    {
        return item != null
            && string.Equals(item.EventType, "status", StringComparison.OrdinalIgnoreCase)
            && item.IsFinal;
    }

    private static int GetBufferedReplayDelayMilliseconds(
        ChatTaskStreamEvent currentItem,
        DateTimeOffset? previousTimestamp)
    {
        if (currentItem == null || !previousTimestamp.HasValue)
        {
            return 0;
        }

        // status/message 事件保持即时，chunk 事件做轻量节奏回放，避免晚订阅时一次性刷屏。
        if (!string.Equals(currentItem.EventType, "chunk", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var rawGapMs = (currentItem.Timestamp - previousTimestamp.Value).TotalMilliseconds;
        if (rawGapMs <= 0 || rawGapMs > 300)
        {
            return 18;
        }

        var normalizedGap = (int)Math.Round(rawGapMs);
        if (normalizedGap < 12)
        {
            return 12;
        }

        if (normalizedGap > 48)
        {
            return 48;
        }

        return normalizedGap;
    }

    private void CleanupStreamIfFinished(int chatTaskId, StreamGroup group)
    {
        if (!group.IsComplete || !group.Subscribers.IsEmpty)
        {
            return;
        }

        _streams.TryRemove(chatTaskId, out _);
    }
}

public sealed class ChatTaskStreamEvent
{
    public string EventType { get; set; } = "chunk";
    public int ChatTaskId { get; set; }
    public int? HistoryId { get; set; }
    public int? FromAgentTemplateId { get; set; }
    public string FromAgentName { get; set; }
    public string ResponseId { get; set; }
    public string Text { get; set; }
    public bool IsFinal { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public int ResponseMilliseconds { get; set; }
    public int RoundIndex { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}
