/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：EditQueue.cs
    文件功能描述：手动编辑与自动化开关的排队，请求或推进运行时由房间持有者统一应用


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 新增 AutomationRemoval 与 EnqueueRemoval：访客移除一条或全部自动化

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

/// <summary>The visitor's on/off switch on one automation card.</summary>
public sealed record AutomationToggle(string Id, bool Enabled);

/// <summary>The visitor removing one automation card (<paramref name="Id"/>), or with a null id all of them ("Clear all").</summary>
public sealed record AutomationRemoval(string? Id);

/// <summary>
/// A visitor's hand edit waiting for whoever owns the room. Done completes with what the edit did. A device edit has
/// Path and Value; an automation's on/off switch has <see cref="Toggle"/> instead (Path empty, Value null).
/// </summary>
public sealed record PendingEdit(string Path, JsonNode? Value, TaskCompletionSource<EditOutcome> Done)
{
    public AutomationToggle? Toggle { get; init; }

    /// <summary>Set instead of <see cref="Toggle"/> when the visitor removes an automation, or all of them.</summary>
    public AutomationRemoval? Removal { get; init; }
}

/// <summary>
/// The per-session queue of hand edits and automation switches. Nothing but the room's owner (the idle path under
/// Gate, a running request's waits, a clock advance) touches the room, so they wait here until it takes them.
/// </summary>
public sealed class EditQueue
{
    readonly object _lock = new();
    readonly List<PendingEdit> _pending = new();
    TaskCompletionSource _wake = NewWake();

    static TaskCompletionSource NewWake() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Adds the edit; the task completes when it has been applied.</summary>
    public Task<EditOutcome> Enqueue(string path, JsonNode value) =>
        Add(new PendingEdit(path, value, new(TaskCreationOptions.RunContinuationsAsynchronously)));

    /// <summary>Adds an automation's on/off switch; the task completes when it has been applied (Error "no_automation" for an unknown id).</summary>
    public Task<EditOutcome> EnqueueToggle(string id, bool enabled) =>
        Add(new PendingEdit("", null, new(TaskCreationOptions.RunContinuationsAsynchronously)) { Toggle = new AutomationToggle(id, enabled) });

    /// <summary>Adds the removal of one automation (or of all, with a null id); the task completes when it has been applied (Error "no_automation" for an unknown id).</summary>
    public Task<EditOutcome> EnqueueRemoval(string? id) =>
        Add(new PendingEdit("", null, new(TaskCreationOptions.RunContinuationsAsynchronously)) { Removal = new AutomationRemoval(id) });

    Task<EditOutcome> Add(PendingEdit edit)
    {
        TaskCompletionSource wake;
        lock (_lock)
        {
            _pending.Add(edit);
            wake = _wake;
        }
        wake.TrySetResult();
        return edit.Done.Task;
    }

    /// <summary>Completes when an edit waits (at once if one already does).</summary>
    public Task WhenPending()
    {
        lock (_lock) return _pending.Count > 0 ? Task.CompletedTask : _wake.Task;
    }

    /// <summary>Everything waiting, oldest first; the queue is empty afterwards.</summary>
    public IReadOnlyList<PendingEdit> TakeAll()
    {
        lock (_lock)
        {
            var all = _pending.ToList();
            _pending.Clear();
            if (_wake.Task.IsCompleted) _wake = NewWake();
            return all;
        }
    }
}
