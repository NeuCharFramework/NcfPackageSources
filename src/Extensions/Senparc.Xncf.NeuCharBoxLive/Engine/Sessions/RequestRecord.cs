/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RequestRecord.cs
    文件功能描述：一次访客请求：回放用的原始房间、批准卡片的回答与结果


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 审批等待为 0 时一直等待，不再过期

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

/// <summary>
/// One visitor request. Before is a copy of the room just before it (a what-if replays from there, spec 3.4).
/// The approval card is answered through <see cref="TryDecide"/>: exactly one answer wins, however many tabs send one.
/// </summary>
public class RequestRecord
{
    readonly TimeProvider _tp;
    readonly object _gate = new();
    TaskCompletionSource<Decision>? _pending;

    public RequestRecord(string rid, string text, RoomState before, TimeProvider tp)
    {
        Rid = rid;
        Text = text;
        Before = before.Clone();
        _tp = tp;
    }

    public string Rid { get; }
    public string Text { get; }
    public RoomState Before { get; }

    /// <summary>The approved plan, re-plans merged in. Null until the visitor approves.</summary>
    public ApprovedPlan? Plan { get; set; }

    /// <summary>Set for a what-if: the device that fails in the replay.</summary>
    public string? WhatIfDevice { get; set; }
    public bool IsWhatIf => WhatIfDevice is not null;

    /// <summary>The done outcome, once the request is over.</summary>
    public string? Outcome { get; set; }
    public Usage Usage { get; set; } = new(0, 0, 0);

    /// <summary>Set when admission already logged the run (Task 9), so the runner doesn't log it twice.</summary>
    public long? RunId { get; set; }

    /// <summary>True while a plan or re-plan card waits for an answer.</summary>
    public bool Waiting { get { lock (_gate) return _pending is not null; } }

    /// <summary>
    /// Opens the card and waits for the answer: null when the timeout (on the TimeProvider) passes first.
    /// The card is open as soon as this returns its task, so call it before emitting <c>awaiting_approval</c>:
    /// an answer that arrives right after the event then finds the card waiting.
    /// </summary>
    public async Task<Decision?> WaitDecisionAsync(TimeSpan timeout, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<Decision>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            if (_pending is not null) throw new InvalidOperationException("a card is already waiting for an answer");
            _pending = tcs;
        }
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            // Zero or less: the card waits for as long as it takes (LiveOptions.ApprovalWait's default).
            var expiry = Task.Delay(timeout > TimeSpan.Zero ? timeout : Timeout.InfiniteTimeSpan, _tp, stop.Token);
            await Task.WhenAny(tcs.Task, expiry);
        }
        finally
        {
            stop.Cancel();
            lock (_gate)
                if (_pending == tcs) _pending = null; // closed: a later answer gets not_waiting
        }
        // An answer that won the lock before the card closed stands, even when the timeout fired at the same moment.
        if (tcs.Task.IsCompletedSuccessfully) return tcs.Task.Result;
        ct.ThrowIfCancellationRequested();
        return null;
    }

    /// <summary>True when this answer was taken; false when no card is waiting (never opened, answered or expired).</summary>
    public bool TryDecide(Decision d)
    {
        lock (_gate)
        {
            if (_pending is null) return false;
            var p = _pending;
            _pending = null;
            p.TrySetResult(d);
            return true;
        }
    }
}
