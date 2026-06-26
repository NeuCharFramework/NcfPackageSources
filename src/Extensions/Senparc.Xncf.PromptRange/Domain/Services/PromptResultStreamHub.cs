using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace Senparc.Xncf.PromptRange.Domain.Services;

public sealed class PromptResultStreamHub
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<PromptResultStreamEvent>>> _subscribers = new(StringComparer.OrdinalIgnoreCase);

    public async IAsyncEnumerable<PromptResultStreamEvent> Subscribe(
        string streamId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            yield break;
        }

        var group = _subscribers.GetOrAdd(streamId, _ => new ConcurrentDictionary<Guid, Channel<PromptResultStreamEvent>>());
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<PromptResultStreamEvent>();
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
                _subscribers.TryRemove(streamId, out _);
            }
        }
    }

    public void Publish(PromptResultStreamEvent streamEvent)
    {
        if (streamEvent == null || string.IsNullOrWhiteSpace(streamEvent.StreamId))
        {
            return;
        }

        if (!_subscribers.TryGetValue(streamEvent.StreamId, out var group) || group.IsEmpty)
        {
            return;
        }

        foreach (var pair in group)
        {
            if (!pair.Value.Writer.TryWrite(streamEvent))
            {
                pair.Value.Writer.TryComplete();
                group.TryRemove(pair.Key, out _);
            }
        }
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
