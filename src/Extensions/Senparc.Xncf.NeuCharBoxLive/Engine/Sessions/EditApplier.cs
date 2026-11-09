/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：EditApplier.cs
    文件功能描述：NeuCharBox Live 引擎（Sessions）：EditApplier


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

/// <summary>The stamps an emitter took for the paths it is about to stream, and the epoch then.</summary>
public sealed record EmitStamps(long Epoch, IReadOnlyDictionary<string, long> Stamps);

/// <summary>
/// Applies the session's queued hand edits to the room. The caller owns the room (Gate, or the running request).
/// Also the session's state-event discipline, shared with the request runner: every state event is appended under
/// EmitGate either as the newest for its path (its stamp bumped with the append) or only while the stamps its emitter
/// took still hold, so the last value the browser gets for a path is the newest one.
/// </summary>
public static class EditApplier
{
    /// <summary>
    /// Applies each pending edit and completes its task. An edit with an Error changes nothing, and emits the room's
    /// current value for its field (and lamp.on for lamp.brightness) as the newest, so a card that already shows the
    /// visitor's value (the edit was answered Queued) goes back to the room's. Otherwise every change with no delay goes
    /// out at once as a state event, and every row as a timeline event, all tagged src "edit" and <paramref name="rid"/>.
    /// A delayed change is emitted later by a background task, paced by <paramref name="pacer"/>, with its own value,
    /// unless something newer (an edit, the runner) has since emitted the same path. A change that a later change in
    /// the same edit (a later rule's fire) overtakes for its path is dropped, so the edit's last frame for a path is its
    /// last value. With <paramref name="immediate"/> every change goes out at once, in order, ignoring its delay
    /// (inside a fast-forward, where sim time is compressed). <paramref name="onRow"/> sees each row after its event is
    /// out, outside EmitGate. An applied edit also streams the automations list when it changed (a rule it fired ran
    /// once more today). An automation's on/off switch is applied in its place in the queue (<see cref="ApplyToggle"/>).
    /// </summary>
    public static void ApplyAll(LiveSession s, RoomEditor editor, RoomState room, string? rid, IPacer pacer,
        Action<TimelineRow>? onRow = null, bool immediate = false)
    {
        foreach (var p in s.Edits.TakeAll())
        {
            if (p.Toggle is { } t)
            {
                ApplyToggle(s, room, rid, p, t);
                continue;
            }

            EditOutcome o;
            try { o = editor.Apply(room, p.Path, p.Value!); }
            catch (Exception e)
            {
                p.Done.TrySetException(e);
                continue;
            }
            if (o.Error is not null)
            {
                foreach (var path in p.Path == "lamp.brightness" ? new[] { p.Path, "lamp.on" } : new[] { p.Path })
                    EmitNewest(s, rid, Ev.FromEdit(Ev.State(new StateChange(path, room.Get(path)?.DeepClone(), 0))), path);
                p.Done.TrySetResult(o);
                continue;
            }

            EmitStamps stamps;
            var delayed = new List<StateChange>();
            var changes = immediate ? o.Changes : WithoutOvertaken(o.Changes);
            var automations = ListOf(s, room); // a rule the edit fired ran once more today
            var ids = Automations.Ids(room).ToList();
            lock (s.EmitGate)
            {
                stamps = TakeStamps(s, changes.Select(c => c.Path));
                foreach (var c in changes)
                {
                    if (c.DelayMs > 0 && !immediate) delayed.Add(c);
                    else Append(s, rid, Ev.FromEdit(Ev.State(c)));
                }
                foreach (var row in o.Rows) Append(s, rid, Ev.FromEdit(Ev.Timeline(row)));
                if (automations is not null) AppendAutomations(s, rid, automations, ids, edit: true, force: false);
            }
            if (onRow is not null) // outside EmitGate: the caller's code never runs under it
                foreach (var row in o.Rows) onRow(row);
            if (delayed.Count > 0) EmitLater(s, rid, pacer, delayed, stamps);
            p.Done.TrySetResult(o);
        }
    }

    /// <summary>
    /// An automation's on/off switch, applied to the owner's room (Automations.SetEnabled's re-enable rules). A known
    /// id always streams the automations list (src "edit", the owner's rid), changed or not, so the card ends on the
    /// room's value; an unknown one (dropped meanwhile) changes nothing and completes with "no_automation".
    /// </summary>
    static void ApplyToggle(LiveSession s, RoomState room, string? rid, PendingEdit p, AutomationToggle t)
    {
        try
        {
            if (!Automations.SetEnabled(room, t.Id, t.Enabled))
            {
                p.Done.TrySetResult(new EditOutcome("no_automation", [], []));
                return;
            }
            EmitAutomations(s, room, rid, edit: true, force: true);
        }
        catch (Exception e)
        {
            p.Done.TrySetException(e);
            return;
        }
        p.Done.TrySetResult(new EditOutcome(null, [], []));
    }

    /// <summary>
    /// Streams the room's automations list (contract: <c>automations</c>) when it differs from the last one streamed, or
    /// always with <paramref name="force"/>. The caller owns <paramref name="room"/>; the list is built outside
    /// EmitGate, and compared and appended under it. <paramref name="edit"/> tags it src "edit" (a hand edit, a toggle,
    /// an advance, a reset). Never call it holding EmitGate's leaf locks.
    /// </summary>
    public static void EmitAutomations(LiveSession s, RoomState room, string? rid, bool edit, bool force = false)
    {
        if (ListOf(s, room) is not { } items) return;
        var ids = Automations.Ids(room).ToList();
        lock (s.EmitGate) AppendAutomations(s, rid, items, ids, edit, force);
    }

    public static readonly EventId AutomationTextFailed = new(7201, nameof(AutomationTextFailed));

    /// <summary>
    /// The room's automations list, never throwing: card text must not fail an edit or a run. An item whose text can't be
    /// built is left out, a list that can't be built at all is null (no event then); each is logged by id and kind of
    /// error only, never with visitor or model text.
    /// </summary>
    public static JsonArray? ListOf(LiveSession s, RoomState room)
    {
        try
        {
            return Automations.List(s.Scene, s.Behavior, room, (id, e) =>
            {
                // Once per automation: the list is rebuilt at every fast-forward step.
                if (s.FirstTextFailure(id))
                    s.Log.LogError(AutomationTextFailed, e, "Automation {Id} in session {Sid} left out of the list: its card text failed", id, s.Id);
            });
        }
        catch (Exception e)
        {
            s.Log.LogError(AutomationTextFailed, e, "The automations list of session {Sid} couldn't be built; no list sent", s.Id);
            return null;
        }
    }

    /// <summary>The append half of <see cref="EmitAutomations"/>, for a caller already under EmitGate (with other events in the same step).</summary>
    public static void AppendAutomations(LiveSession s, string? rid, JsonArray items, IEnumerable<string> ids, bool edit, bool force)
    {
        lock (s.EmitGate)
        {
            var json = items.ToJsonString();
            if (!force && json == s.LastAutomations) return;
            s.SetLastAutomations(json, ids);
            var e = Ev.Automations(items);
            Append(s, rid, edit ? Ev.FromEdit(e) : e);
        }
    }

    /// <summary>
    /// The changes without each one that a later change for the same path goes out no later than (paced by DelayMs):
    /// two fires setting one path (a lamp turned off with a delayed lamp.on frame, then set to 50 % at once) would
    /// otherwise end on the earlier fire's frame. One action's own frames for a path (pump on now, off later) all stay.
    /// </summary>
    static List<StateChange> WithoutOvertaken(IReadOnlyList<StateChange> changes) =>
        changes.Where((c, i) => !changes.Skip(i + 1).Any(later => later.Path == c.Path && later.DelayMs <= c.DelayMs)).ToList();

    /// <summary>Bumps the stamp of each path, so every frame stamped for them before is now older, and returns the new stamps.</summary>
    public static EmitStamps TakeStamps(LiveSession s, IEnumerable<string> paths)
    {
        lock (s.EmitGate) return new(s.Epoch, paths.Distinct().ToDictionary(p => p, s.BumpEditStamp));
    }

    /// <summary>Appends a state event now as the newest for its path: the bump and the append are one step under EmitGate.</summary>
    public static void EmitNewest(LiveSession s, string? rid, (string kind, JsonObject data) e, string path)
    {
        lock (s.EmitGate)
        {
            s.BumpEditStamp(path);
            Append(s, rid, e);
        }
    }

    /// <summary>Appends the event only while nothing newer has emitted its path since <paramref name="st"/> was taken, and no reset.</summary>
    public static void EmitIfNewest(LiveSession s, string? rid, (string kind, JsonObject data) e, string path, EmitStamps st)
    {
        lock (s.EmitGate)
            if (s.Epoch == st.Epoch && s.EditStamp(path) == st.Stamps[path]) Append(s, rid, e);
    }

    static void Append(LiveSession s, string? rid, (string kind, JsonObject data) e) => s.Events.Append(e.kind, rid, e.data);

    static void EmitLater(LiveSession s, string? rid, IPacer pacer, List<StateChange> delayed, EmitStamps stamps)
    {
        using (ExecutionContext.SuppressFlow())
            _ = Task.Run(async () =>
            {
                var prev = 0;
                foreach (var c in delayed.OrderBy(c => c.DelayMs))
                {
                    await pacer.WaitAsync(TimeSpan.FromMilliseconds(c.DelayMs - prev), CancellationToken.None);
                    prev = c.DelayMs;
                    EmitIfNewest(s, rid, Ev.FromEdit(Ev.State(c)), c.Path, stamps);
                }
            });
    }
}
