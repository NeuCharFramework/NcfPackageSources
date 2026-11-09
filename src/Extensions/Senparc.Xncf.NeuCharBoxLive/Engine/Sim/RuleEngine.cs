/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RuleEngine.cs
    文件功能描述：NeuCharBox Live 引擎（Sim）：RuleEngine


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

/// <summary>A timeline row and the state changes its action made (empty for notify, held and fault rows).</summary>
public record RuleFire(TimelineRow Row, IReadOnlyList<StateChange> Changes);

/// <summary>Evaluates rules and runs actions for the fast-forward and for hand edits.</summary>
public sealed class RuleEngine(Simulator sim)
{
    const double Eps = 1e-9, RuleGapHours = 6;

    /// <summary>
    /// One clock step: fires the rule when its condition holds (6 h gap, per-day cap). <paramref name="v"/> is the
    /// watched value as the step left it before any rule fired, so a fire earlier in the step doesn't hide or trigger
    /// another rule (as with a hand edit). An off rule never fires, nor reports its sensor's fault; a "becomes" rule
    /// still notes the value while off. A below/above rule switched back on fires at the first tick that finds its value in
    /// the zone, like one just made, with the 6 h gap and the per-day cap as before.
    /// </summary>
    public RuleFire? Tick(Rule r, JsonNode? v, string at, int day, RoomState room, HashSet<(string, int)> faultDays)
    {
        if (!r.Enabled)
        {
            if (r.Op == RuleOp.Becomes) r.LastSeen = v?.DeepClone();
            return null;
        }
        var sensor = r.Path[..r.Path.IndexOf('.')];
        if (room.Failed.Contains(sensor)) return FaultRow(sensor, r.Step, at, day, faultDays, "rule", r.Id);

        bool due;
        if (r.Op == RuleOp.Becomes)
        {
            due = JsonNode.DeepEquals(v, r.Value) && !JsonNode.DeepEquals(r.LastSeen, r.Value);
            r.LastSeen = v?.DeepClone();
        }
        else
        {
            due = InZone(r, v)
                && (r.LastFiredHour is not double last || room.Hour - last >= RuleGapHours - Eps)
                && r.FiredPerDay.GetValueOrDefault(day) < r.MaxPerDay;
        }
        if (!due) return null;

        // A failed fire counts as a fire.
        r.LastFiredHour = room.Hour;
        r.FiredPerDay[day] = r.FiredPerDay.GetValueOrDefault(day) + 1;
        return Run(r, at, day, room, faultDays);
    }

    /// <summary>
    /// After a hand edit: a below/above rule fires when the edit moved its value into the zone (no 6 h gap); a
    /// "becomes" rule fires when its value was just reached. <paramref name="before"/> and <paramref name="after"/> are the watched value just before and just after the edit, before any rule fired.
    /// An off rule never fires; a "becomes" rule still notes the value while off.
    /// </summary>
    public RuleFire? AfterEdit(Rule r, JsonNode? before, JsonNode? after, string at, int day, RoomState room)
    {
        if (!r.Enabled)
        {
            if (r.Op == RuleOp.Becomes) r.LastSeen = after?.DeepClone();
            return null;
        }
        var sensor = r.Path[..r.Path.IndexOf('.')];
        if (room.Failed.Contains(sensor)) return null; // a hand edit never produces a fault

        var v = after;
        bool due;
        if (r.Op == RuleOp.Becomes)
        {
            due = JsonNode.DeepEquals(v, r.Value) && !JsonNode.DeepEquals(r.LastSeen, r.Value);
            r.LastSeen = v?.DeepClone();
        }
        else
            due = InZone(r, v) && !InZone(r, before);
        if (!due) return null;

        if (r.FiredPerDay.GetValueOrDefault(day) >= r.MaxPerDay)
        {
            var device = r.Then is null ? null : sim.Manifest.Action(r.Then.Action)?.Device;
            var text = r.MaxPerDay == 1 ? "It already ran once today" : $"It already ran {r.MaxPerDay} times today";
            return new(new TimelineRow(at, text, true, "notify", device, r.Step, "held", r.Id), []);
        }

        r.LastFiredHour = room.Hour;
        r.FiredPerDay[day] = r.FiredPerDay.GetValueOrDefault(day) + 1;
        return Run(r, at, day, room, []);
    }

    /// <summary>
    /// Runs one action (a rule's or a schedule's). <paramref name="origin"/> is "rule" or "schedule", and
    /// <paramref name="automation"/> is that rule's or schedule's id, for the row.
    /// </summary>
    public RuleFire? Fire(ActionCall call, string step, string at, int day, RoomState room, HashSet<(string, int)> faultDays, string origin,
        string? automation = null)
    {
        var spec = sim.Manifest.Action(call.Action);
        var o = sim.Execute(call, room);
        if (o.Ok) return new(new TimelineRow(at, sim.Behavior.Label(spec!, call.Args), true, "action", spec!.Device, step, origin, automation), o.Changes);
        if (o.Outcome == Outcome.Failed) return FaultRow(spec!.Device, step, at, day, faultDays, origin, automation);
        return new(new TimelineRow(at, $"{call.Action}: {o.Text}", false, "action", spec?.Device, step, origin, automation), o.Changes);
    }

    RuleFire? Run(Rule r, string at, int day, RoomState room, HashSet<(string, int)> faultDays)
    {
        if (r.Then is not null) return Fire(r.Then, r.Step, at, day, room, faultDays, "rule", r.Id);
        return r.Notify is null ? null : new(new TimelineRow(at, r.Notify, true, "notify", null, r.Step, "rule", r.Id), []);
    }

    /// <summary>At most one fault row per device per sim day, whether it failed as a sensor or an actuator.</summary>
    RuleFire? FaultRow(string device, string step, string at, int day, HashSet<(string, int)> faultDays, string origin, string? automation) =>
        faultDays.Add((device, day))
            ? new(new TimelineRow(at, $"{sim.Manifest.Device(device)?.Name ?? device} not responding", false, "fault", device, step, origin, automation), [])
            : null;

    /// <summary>A below/above rule's condition holds for <paramref name="v"/>. Never true for a "becomes" rule or a non-number.</summary>
    public static bool InZone(Rule r, JsonNode? v) =>
        r.Op != RuleOp.Becomes && Number(v) is double x && Number(r.Value) is double limit && (r.Op == RuleOp.Below ? x < limit : x > limit);

    static double? Number(JsonNode? n) =>
        n is JsonValue v && v.GetValueKind() == JsonValueKind.Number
            ? double.Parse(v.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture)
            : null;
}
