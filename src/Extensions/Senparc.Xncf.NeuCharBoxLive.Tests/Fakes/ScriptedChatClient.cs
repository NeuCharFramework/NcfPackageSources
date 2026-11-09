/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ScriptedChatClient.cs
    文件功能描述：测试替身：按脚本回答的对话客户端，免去真实模型调用


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;

/// <summary>
/// A fake model: each call takes the next scripted turn, in order, and records what it was sent. It never calls a
/// real model. A turn may throw to act as a failing service.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    /// <summary>A turn that returns this makes its call wait until the call's token is cancelled. A stream yields "I " first.</summary>
    public static readonly ChatResponse Stalled = new(new ChatMessage(ChatRole.Assistant, "I stalled"));

    static int _ids;

    readonly Func<IReadOnlyList<ChatMessage>, ChatOptions?, ChatResponse>[] _turns;
    readonly object _gate = new();
    readonly TaskCompletionSource _stallStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _next;

    public ScriptedChatClient(params Func<IReadOnlyList<ChatMessage>, ChatOptions?, ChatResponse>[] turns) => _turns = turns;

    /// <summary>Every call made, with a snapshot of its messages.</summary>
    public List<(IReadOnlyList<ChatMessage> Messages, ChatOptions? Options)> Calls { get; } = new();

    /// <summary>Completes when a call reaches a Stalled turn, so a test knows when to move the clock.</summary>
    public Task StallStarted => _stallStarted.Task;

    public static ChatResponse Tool(string name, object args, int inTok = 100, int outTok = 20) =>
        WithUsage(new ChatResponse(new ChatMessage(ChatRole.Assistant, new List<AIContent> { Call(name, args) })), inTok, outTok);

    public static ChatResponse Tools(params (string, object)[] calls) =>
        WithUsage(new ChatResponse(new ChatMessage(ChatRole.Assistant, calls.Select(c => (AIContent)Call(c.Item1, c.Item2)).ToList())), 100, 20);

    public static ChatResponse Text(string t) => WithUsage(new ChatResponse(new ChatMessage(ChatRole.Assistant, t)), 100, 20);

    /// <summary>The args are sent the way a real client parses them: a dictionary of JsonElement values.</summary>
    static FunctionCallContent Call(string name, object args)
    {
        var json = JsonSerializer.Serialize(args);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        return new FunctionCallContent($"call_{Interlocked.Increment(ref _ids)}", name, dict);
    }

    static ChatResponse WithUsage(ChatResponse r, int inTok, int outTok)
    {
        r.Usage = new UsageDetails { InputTokenCount = inTok, OutputTokenCount = outTok, TotalTokenCount = inTok + outTok };
        return r;
    }

    ChatResponse Next(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var snapshot = messages.ToList();
        Func<IReadOnlyList<ChatMessage>, ChatOptions?, ChatResponse> turn;
        lock (_gate)
        {
            Calls.Add((snapshot, options));
            if (_next >= _turns.Length) throw new InvalidOperationException($"no scripted turn {_next + 1}");
            turn = _turns[_next++];
        }
        return turn(snapshot, options);
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var r = Next(messages, options);
        if (ReferenceEquals(r, Stalled)) await StallAsync(cancellationToken);
        return r;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var r = Next(messages, options);
        var words = Regex.Matches(r.Text, @"\S+\s*").Select(m => m.Value).ToList();
        if (ReferenceEquals(r, Stalled))
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, words[0]);
            await StallAsync(cancellationToken);
        }
        foreach (var w in words) yield return new ChatResponseUpdate(ChatRole.Assistant, w);
        if (r.Usage is not null) yield return new ChatResponseUpdate(ChatRole.Assistant, new List<AIContent> { new UsageContent(r.Usage) });
    }

    /// <summary>Its continuation runs on the thread pool, not inside the FakeTimeProvider.Advance that cancels it.</summary>
    async Task StallAsync(CancellationToken ct)
    {
        _stallStarted.TrySetResult();
        var wait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (ct.Register(() => wait.TrySetCanceled(ct))) await wait.Task;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
