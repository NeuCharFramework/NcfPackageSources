/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：TestDoubles.cs
    文件功能描述：测试替身：可控的时间、节奏器与运行时快照等共用替身


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 SetChat 只换模型（保留关闭原因、来源与限额）；ListLoggerProvider 移到这里供多个测试共用

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

    /// <summary>
    /// Both models on another chat client, keeping their names (<see cref="Model"/> where there was none) and the rest of the
    /// snapshot: limits, origins and off reason.
    /// </summary>
    public static void SetChat(this StaticLiveRuntime rt, IChatClient chat) => rt.Current = rt.Current with
    {
        Plan = new LiveModel(rt.Current.Plan?.Name ?? Model, chat),
        Run = new LiveModel(rt.Current.Run?.Name ?? Model, chat),
    };
}

/// <summary>Keeps every host log entry in memory (register it as an ILoggerProvider), so a test can look for errors.</summary>
public sealed class ListLoggerProvider : ILoggerProvider
{
    /// <summary>Lock it while reading: the host may still be logging.</summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Logger(this);

    public void Dispose() { }

    sealed class Logger(ListLoggerProvider p) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (p.Entries) p.Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
