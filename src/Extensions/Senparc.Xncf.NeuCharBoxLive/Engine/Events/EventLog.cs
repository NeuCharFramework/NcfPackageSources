/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：EventLog.cs
    文件功能描述：会话内只追加的事件日志，支持按序号续传与等待新事件


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Events;

/// <summary>Append-only per-session event list. Seq = list index + 1, so resuming is a slice.</summary>
public class EventLog(TimeProvider tp)
{
    readonly object gate = new();
    readonly List<LiveEvent> events = new();
    TaskCompletionSource wake = NewWake();

    static TaskCompletionSource NewWake() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    public long LastSeq { get { lock (gate) return events.Count; } }

    public LiveEvent Append(string kind, string? rid, JsonObject data)
    {
        TaskCompletionSource old;
        LiveEvent e;
        lock (gate)
        {
            e = new LiveEvent(events.Count + 1, kind, rid, data);
            events.Add(e);
            old = wake;
            wake = NewWake();
        }
        old.TrySetResult();
        return e;
    }

    /// <summary>Events with Seq greater than after. Waits up to maxWait when there are none; empty list on timeout.</summary>
    public async Task<IReadOnlyList<LiveEvent>> WaitAfterAsync(long after, TimeSpan maxWait, CancellationToken ct)
    {
        Task signal;
        lock (gate)
        {
            var now = Slice(after);
            if (now.Count > 0) return now;
            signal = wake.Task;
        }
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var delay = Task.Delay(maxWait, tp, stop.Token);
        var first = await Task.WhenAny(signal, delay);
        stop.Cancel();
        if (first == delay) await delay; // throws OperationCanceledException when ct fired; a timeout falls through
        lock (gate) return Slice(after);
    }

    IReadOnlyList<LiveEvent> Slice(long after)
    {
        var from = (int)Math.Clamp(after, 0, events.Count);
        return events.GetRange(from, events.Count - from);
    }
}
