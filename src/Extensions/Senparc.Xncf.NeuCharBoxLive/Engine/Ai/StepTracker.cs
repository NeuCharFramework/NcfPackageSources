/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：StepTracker.cs
    文件功能描述：NeuCharBox Live 引擎（Ai）：StepTracker


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Events;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

/// <summary>
/// The live checklist's step statuses. A step is running from its first call. A call for another step closes every
/// running step: done when none of its devices is left failed, else failed. A failed call is cleared by a later ok
/// call to the same device in that step (a retry). A null step (a stepless read or safe-state call) touches nothing.
/// </summary>
public class StepTracker
{
    enum Status { Untouched, Running, Done, Failed, Skipped }

    readonly EventLog _log;
    readonly string _rid;
    readonly ApprovedPlan _plan;
    readonly object _gate = new();
    readonly Dictionary<string, Status> _status = new();
    readonly Dictionary<string, HashSet<string>> _failing = new();

    public StepTracker(EventLog log, string rid, ApprovedPlan plan)
    {
        _log = log;
        _rid = rid;
        _plan = plan;
    }

    /// <summary>Closes the other running steps, then marks this one running. Steps a re-plan added are tracked too.</summary>
    public void OnCall(string? step)
    {
        if (step is null || _plan.Step(step) is null) return;
        lock (_gate)
        {
            foreach (var other in Ordered().Where(id => id != step && Get(id) == Status.Running).ToList()) Close(other);
            if (Get(step) == Status.Running) return;
            _status[step] = Status.Running;
            Emit(step, "running");
        }
    }

    /// <summary>Device keys the retry rule; without one, the step's last result decides.</summary>
    public void OnResult(string? step, bool ok, string? device = null)
    {
        if (step is null || _plan.Step(step) is null) return;
        lock (_gate)
        {
            if (!_failing.TryGetValue(step, out var set)) _failing[step] = set = new HashSet<string>();
            if (ok) set.Remove(device ?? "");
            else set.Add(device ?? "");
        }
    }

    /// <summary>Closes the running steps and marks untouched ones skipped. Emits nothing the second time.</summary>
    public void Finish()
    {
        lock (_gate)
            foreach (var id in Ordered())
                switch (Get(id))
                {
                    case Status.Running: Close(id); break;
                    case Status.Untouched: _status[id] = Status.Skipped; Emit(id, "skipped"); break;
                }
    }

    /// <summary>
    /// A step whose schedule or rule hit a device that didn't respond while time passed: failed now, once. The device
    /// counts as failing in the step, so a later retry rule still applies.
    /// </summary>
    public void Fail(string? step, string? device)
    {
        if (step is null || _plan.Step(step) is null) return;
        lock (_gate)
        {
            if (!_failing.TryGetValue(step, out var set)) _failing[step] = set = new HashSet<string>();
            set.Add(device ?? "");
            if (Get(step) == Status.Failed) return;
            _status[step] = Status.Failed;
            Emit(step, "failed");
        }
    }

    public bool AnyFailed { get { lock (_gate) return _status.Values.Any(s => s == Status.Failed); } }

    /// <summary>The last status sent for the step: "running", "done", "failed", "skipped", or null when untouched.</summary>
    public string? StatusOf(string step)
    {
        lock (_gate)
            return Get(step) switch
            {
                Status.Running => "running",
                Status.Done => "done",
                Status.Failed => "failed",
                Status.Skipped => "skipped",
                _ => null,
            };
    }

    IEnumerable<string> Ordered() => _plan.Steps.Select(s => s.Id);

    Status Get(string id) => _status.GetValueOrDefault(id, Status.Untouched);

    void Close(string id)
    {
        var failed = _failing.TryGetValue(id, out var set) && set.Count > 0;
        _status[id] = failed ? Status.Failed : Status.Done;
        Emit(id, failed ? "failed" : "done");
    }

    void Emit(string id, string status)
    {
        var (kind, data) = Ev.Step(id, status);
        _log.Append(kind, _rid, data);
    }
}
