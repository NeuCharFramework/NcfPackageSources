/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IPacer.cs
    文件功能描述：节奏器接口与实时实现：在流式事件之间等待，便于访客观看


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

/// <summary>Real-time waits between streamed events, so a visitor can watch them.</summary>
public interface IPacer
{
    Task WaitAsync(TimeSpan d, CancellationToken ct);
}

public sealed class RealPacer : IPacer
{
    public Task WaitAsync(TimeSpan d, CancellationToken ct) => d > TimeSpan.Zero ? Task.Delay(d, ct) : Task.CompletedTask;
}

/// <summary>
/// Returns at once and adds each wait to Total. Tests pass a FakeTimeProvider's Advance as onWait,
/// so the fake clock sees pacer time.
/// </summary>
public sealed class InstantPacer : IPacer
{
    readonly Action<TimeSpan>? _onWait;
    readonly object _gate = new();
    TimeSpan _total;

    public InstantPacer(Action<TimeSpan>? onWait = null) => _onWait = onWait;

    public TimeSpan Total { get { lock (_gate) return _total; } }

    public Task WaitAsync(TimeSpan d, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return Task.FromCanceled(ct);
        if (d <= TimeSpan.Zero) return Task.CompletedTask;
        lock (_gate) _total += d;
        _onWait?.Invoke(d);
        return Task.CompletedTask;
    }
}
