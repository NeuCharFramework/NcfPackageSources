/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RequestBudget.cs
    文件功能描述：单个请求的调用数与工作时间预算


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 调用数或工作时间上限为 0 时不设限（TimeUp、Remaining 为无限）

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

/// <summary>Kind is the contract's limit kind: "calls" or "time".</summary>
public class BudgetExceededException(string kind) : Exception($"request {kind} budget exceeded")
{
    public string Kind { get; } = kind;
}

/// <summary>
/// One request's bounds (spec 6): model calls, and server work time with approval waits left out; a bound of zero or
/// less is no bound (the default: a request is never cut short). Also adds up the
/// tokens for done.usage. Time runs on the TimeProvider, so a FakeTimeProvider drives it.
/// </summary>
public class RequestBudget
{
    readonly object _gate = new();
    readonly int _maxCalls;
    readonly TimeSpan _maxWork;
    readonly TimeProvider _tp;
    readonly long _start;
    TimeSpan _paused;
    long? _pauseStart;
    int _calls, _prompt, _completion;

    public RequestBudget(int maxCalls, TimeSpan maxWork, TimeProvider tp)
    {
        _maxCalls = maxCalls;
        _maxWork = maxWork;
        _tp = tp;
        _start = tp.GetTimestamp();
    }

    /// <summary>Counts a model call about to be made; throws when the calls or the work time are used up.</summary>
    public void BeforeCall()
    {
        lock (_gate)
        {
            if (_maxCalls > 0 && _calls >= _maxCalls) throw new BudgetExceededException("calls");
            if (_maxWork > TimeSpan.Zero && WorkedLocked() >= _maxWork) throw new BudgetExceededException("time");
            _calls++;
        }
    }

    /// <summary>Stops the work clock until the returned handle is disposed.</summary>
    public IDisposable PauseForApproval()
    {
        lock (_gate)
        {
            if (_pauseStart is not null) throw new InvalidOperationException("already paused");
            _pauseStart = _tp.GetTimestamp();
        }
        return new Resume(this);
    }

    /// <summary>A call's reported tokens, or the estimates where the service reported none.</summary>
    public void AddUsage(UsageDetails? u, int estPrompt, int estCompletion)
    {
        lock (_gate)
        {
            _prompt += Clamp(u?.InputTokenCount) ?? estPrompt;
            _completion += Clamp(u?.OutputTokenCount) ?? estCompletion;
        }
    }

    /// <summary>The work time is used up (never, with no bound on it).</summary>
    public bool TimeUp => _maxWork > TimeSpan.Zero && Remaining <= TimeSpan.Zero;

    /// <summary>The work time left, never negative; <see cref="Timeout.InfiniteTimeSpan"/> with no bound on it (a timer given it never fires).</summary>
    public TimeSpan Remaining
    {
        get
        {
            if (_maxWork <= TimeSpan.Zero) return Timeout.InfiniteTimeSpan;
            lock (_gate)
            {
                var left = _maxWork - WorkedLocked();
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }
    }

    public Usage Usage { get { lock (_gate) return new(_prompt, _completion, _calls); } }

    TimeSpan WorkedLocked()
    {
        var paused = _paused + (_pauseStart is long p ? _tp.GetElapsedTime(p) : TimeSpan.Zero);
        return _tp.GetElapsedTime(_start) - paused;
    }

    static int? Clamp(long? n) => n is long v ? (int)Math.Clamp(v, 0, int.MaxValue) : null;

    sealed class Resume(RequestBudget b) : IDisposable
    {
        bool _done;

        public void Dispose()
        {
            lock (b._gate)
            {
                if (_done || b._pauseStart is not long p) return;
                _done = true;
                b._paused += b._tp.GetElapsedTime(p);
                b._pauseStart = null;
            }
        }
    }
}
