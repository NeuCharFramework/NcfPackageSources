/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ChatTaskStreamHub.cs
    文件功能描述：ChatTaskStreamHub 服务逻辑
    
    
    创建标识：Senparc - 20260626
    
    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

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
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Channel<ChatTaskStreamEvent>>> _subscribers = new();

    public async IAsyncEnumerable<ChatTaskStreamEvent> Subscribe(
        int chatTaskId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var group = _subscribers.GetOrAdd(chatTaskId, _ => new ConcurrentDictionary<Guid, Channel<ChatTaskStreamEvent>>());
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<ChatTaskStreamEvent>();

        group[subscriptionId] = channel;

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return item;
            }
        }
        finally
        {
            group.TryRemove(subscriptionId, out _);
            if (group.IsEmpty)
            {
                _subscribers.TryRemove(chatTaskId, out _);
            }
        }
    }

    public void Publish(ChatTaskStreamEvent item)
    {
        if (item == null)
        {
            return;
        }

        if (!_subscribers.TryGetValue(item.ChatTaskId, out var group) || group.IsEmpty)
        {
            return;
        }

        foreach (var pair in group)
        {
            if (!pair.Value.Writer.TryWrite(item))
            {
                pair.Value.Writer.TryComplete();
                group.TryRemove(pair.Key, out _);
            }
        }
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
