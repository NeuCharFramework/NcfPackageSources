/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AgentExecutionStreamHub.cs
    文件功能描述：独立 Agent 执行任务的实时事件流

    创建标识：Senparc - 20260822

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 支持独立 Agent 执行详情和过程回放

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 新增独立 Agent 执行任务持久化、管理页和 SSE 过程回放


----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

public sealed class AgentExecutionRuntimeStore
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _tokens = new();

    public void Register(int taskId, CancellationTokenSource cancellationTokenSource)
    {
        if (taskId > 0 && cancellationTokenSource != null)
        {
            _tokens[taskId] = cancellationTokenSource;
        }
    }

    public bool TryCancel(int taskId)
    {
        return taskId > 0
            && _tokens.TryGetValue(taskId, out var cancellationTokenSource)
            && TryCancel(cancellationTokenSource);
    }

    public void Remove(int taskId)
    {
        if (_tokens.TryRemove(taskId, out var cancellationTokenSource))
        {
            cancellationTokenSource.Dispose();
        }
    }

    private static bool TryCancel(CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            cancellationTokenSource.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}

public sealed class AgentExecutionStreamHub
{
    private sealed class StreamGroup
    {
        public readonly object Sync = new();
        public readonly List<AgentExecutionStreamEvent> Buffer = new();
        public readonly ConcurrentDictionary<Guid, Channel<AgentExecutionStreamEvent>> Subscribers = new();
        public bool IsComplete;
    }

    private readonly ConcurrentDictionary<int, StreamGroup> _streams = new();

    public async IAsyncEnumerable<AgentExecutionStreamEvent> Subscribe(
        int taskId,
        bool replayBuffered = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (taskId <= 0)
        {
            yield break;
        }

        var group = _streams.GetOrAdd(taskId, _ => new StreamGroup());
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<AgentExecutionStreamEvent>();
        group.Subscribers[subscriptionId] = channel;

        List<AgentExecutionStreamEvent> bufferedEvents;
        lock (group.Sync)
        {
            bufferedEvents = new List<AgentExecutionStreamEvent>(group.Buffer);
        }

        foreach (var bufferedEvent in bufferedEvents)
        {
            if (!replayBuffered)
            {
                break;
            }

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
            CleanupIfFinished(taskId, group);
        }
    }

    public void Publish(AgentExecutionStreamEvent item)
    {
        if (item == null || item.AgentExecutionTaskId <= 0)
        {
            return;
        }

        var group = _streams.GetOrAdd(item.AgentExecutionTaskId, _ => new StreamGroup());
        lock (group.Sync)
        {
            group.Buffer.Add(item);
            if (item.IsFinal)
            {
                group.IsComplete = true;
            }
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
            foreach (var pair in group.Subscribers)
            {
                pair.Value.Writer.TryComplete();
            }

            CleanupIfFinished(item.AgentExecutionTaskId, group);
        }
    }

    private void CleanupIfFinished(int taskId, StreamGroup group)
    {
        if (!group.IsComplete || !group.Subscribers.IsEmpty)
        {
            return;
        }

        _streams.TryRemove(taskId, out _);
    }
}

public sealed class AgentExecutionStreamEvent
{
    public int AgentExecutionTaskId { get; set; }
    public int Sequence { get; set; }
    public string EventType { get; set; } = "info";
    public string Status { get; set; }
    public string Message { get; set; }
    public string ToolName { get; set; }
    public string ToolArguments { get; set; }
    public string ToolResult { get; set; }
    public string ErrorMessage { get; set; }
    public string ResponseId { get; set; }
    public string Text { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public int ResponseMilliseconds { get; set; }
    public bool IsFinal { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}
