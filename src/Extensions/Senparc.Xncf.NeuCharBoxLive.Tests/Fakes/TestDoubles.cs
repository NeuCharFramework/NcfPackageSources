/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：TestDoubles.cs
    文件功能描述：NeuCharBox Live 测试：TestDoubles


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

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

    public void Add(DateOnly day, int tokens) => Interlocked.Add(ref _added, tokens);
}
