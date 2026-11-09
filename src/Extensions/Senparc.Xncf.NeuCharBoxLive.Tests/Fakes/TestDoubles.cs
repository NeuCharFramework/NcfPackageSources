/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：TestDoubles.cs
    文件功能描述：NeuCharBox Live 测试：TestDoubles


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;

/// <summary>Keeps every log entry in memory, so tests can count them and the test output stays quiet.</summary>
public sealed class ListLogger<T> : ILogger<T>
{
    readonly List<(LogLevel Level, EventId Id, string Message)> _entries = new();

    public IReadOnlyList<(LogLevel Level, EventId Id, string Message)> Entries { get { lock (_entries) return _entries.ToList(); } }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_entries) _entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}

public sealed class StubMeter : ITokenMeter
{
    long _added;

    public bool IsSpent { get; set; }
    public long Added => Interlocked.Read(ref _added);

    public bool Spent(DateOnly day) => IsSpent;

    public bool Spent(DateOnly day, long budget) => IsSpent;

    public void Add(DateOnly day, int tokens) => Interlocked.Add(ref _added, tokens);
}

/// <summary>Snapshots for tests that build the engine by hand; <see cref="StaticLiveRuntime"/> is the runtime they set.</summary>
public static class Snapshots
{
    /// <summary>The model name a test's snapshot plans and runs on, unless it names another.</summary>
    public const string Model = "gpt-5-chat";

    /// <summary>On: Plan and Run both <paramref name="model"/> on <paramref name="chat"/>, on the built-in and local origins.</summary>
    public static LiveSnapshot On(IChatClient chat, LiveLimits? limits = null, string model = Model)
    {
        var m = new LiveModel(model, chat);
        return new LiveSnapshot(limits ?? LiveLimits.Defaults, m, m, LiveHost.Origins, null);
    }

    /// <summary>The same snapshot with other limits.</summary>
    public static void SetLimits(this StaticLiveRuntime rt, Func<LiveLimits, LiveLimits> change) =>
        rt.Current = rt.Current with { Limits = change(rt.Current.Limits) };

    /// <summary>The same limits with another chat client (both models).</summary>
    public static void SetChat(this StaticLiveRuntime rt, IChatClient chat) => rt.Current = On(chat, rt.Current.Limits);
}
