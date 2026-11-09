/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FastForward.cs
    文件功能描述：快进：推进模拟时间并触发规则与定时，输出时间线与终态读数


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

/// <summary>Real seconds per idle sim hour and per timeline row. A run spends at most WeekCapSeconds × hours/168 + 5 s.</summary>
public record FastForwardPacing(double IdleHourSeconds = 0.1, double EventSeconds = 0.6, double WeekCapSeconds = 25);

/// <summary>EndReadings is the room at the end, numbers rounded; a failed device shows only its error.</summary>
public record FastForwardReport(IReadOnlyList<TimelineRow> Rows, JsonObject EndReadings, bool Paused);

public interface IFastForwardSink
{
    Task ClockAsync(Clock c);
    Task StateAsync(StateChange s);
    Task RowAsync(TimelineRow r);

    /// <summary>
    /// Called once per device, right after the first fault row that hits it. The sink emits the contract's
    /// <c>fault</c> event, then sets and emits <c>&lt;device&gt;.status = "fault"</c>, then runs the model's
    /// fault turn. True continues the clock; false stops the run with Paused.
    /// </summary>
    Task<bool> FirstFaultAsync(string device, TimelineRow r);

    /// <summary>
    /// Called once a step's rows are out (and before a paused run returns): rules and schedules may have fired, or the
    /// day turned, so the sink can stream the automations list if it changed.
    /// </summary>
    Task StepDoneAsync() => Task.CompletedTask;
}

/// <summary>
/// Moves the sim clock forward without the model: dynamics, schedules and rules. Event-driven: each step jumps to
/// the next whole hour, schedule fire, scene happening or the end, whichever comes first.
/// </summary>
public class FastForward
{
    public const double MaxHours = 168;
    const double Eps = 1e-9, DriftStep = 0.5, CapSlackSeconds = 5;

    readonly Simulator _sim;
    readonly RuleEngine _rules;
    readonly ISceneBehavior _b;
    readonly IPacer _pacer;
    readonly FastForwardPacing _pacing;

    public FastForward(Simulator sim, ISceneBehavior b, IPacer pacer, FastForwardPacing pacing)
    {
        _sim = sim; _rules = new RuleEngine(sim); _b = b; _pacer = pacer; _pacing = pacing;
    }

    DateTime StartDay => _sim.Manifest.StartDay;

    public async Task<FastForwardReport> RunAsync(RoomState room, double hours, IFastForwardSink sink, CancellationToken ct)
    {
        if (!(hours > 0 && hours <= MaxHours))
            throw new ArgumentOutOfRangeException(nameof(hours), hours, $"hours must be more than 0 and at most {MaxHours}");
        var end = room.Hour + hours;
        var cap = _pacing.WeekCapSeconds * hours / MaxHours + CapSlackSeconds;
        double spent = 0;
        var sent = (JsonObject)room.Devices.DeepClone(); // what the browser has
        var drifting = new HashSet<string>();
        var faultDays = new HashSet<(string Device, int Day)>();
        var rows = new List<TimelineRow>();

        while (room.Hour < end - Eps)
        {
            ct.ThrowIfCancellationRequested();
            var prev = room.Hour;
            var next = Math.Min(end, Math.Floor(prev) + 1);
            foreach (var s in room.Schedules)
                if (s.Enabled && NextFire(s, prev) is double t) next = Math.Min(next, t);
            if (_b.NextHappening(room) is double h && h > prev + Eps) next = Math.Min(next, h);

            var happenings = _b.Advance(room, prev, next);
            room.Hour = next;
            var at = SimClock.At(StartDay, next);
            var day = (int)Math.Floor(next / 24 + Eps);
            var step = new List<TimelineRow>();

            // Happenings first, so this interval's schedules and rules see this interval's state.
            foreach (var hp in happenings)
            {
                foreach (var c in hp.Changes)
                {
                    room.Set(c.Path, c.Value);
                    if (Number(c.Value) is not null) drifting.Add(c.Path);
                }
                if (hp.Row is not null) step.Add(hp.Row);
            }
            // An off schedule never runs; switched back on, it runs at its next time (a passed one-off doesn't catch up).
            foreach (var s in room.Schedules.ToList())
            {
                if (!s.Enabled || NextFire(s, prev) is not double t || t > next + Eps) continue;
                s.FiredPerDay[day] = s.FiredPerDay.GetValueOrDefault(day) + 1;
                if (_rules.Fire(s.Call, s.Step, at.Label, day, room, faultDays, "schedule", s.Id) is { } fired) step.Add(fired.Row);
            }
            // Every rule reads the values the happenings and schedules left, not what an earlier rule's fire changed.
            var rules = room.Rules.ToList();
            var watched = rules.Select(r => room.Get(r.Path)?.DeepClone()).ToList();
            for (var i = 0; i < rules.Count; i++)
                if (_rules.Tick(rules[i], watched[i], at.Label, day, room, faultDays) is { } fired)
                    step.Add(fired.Row);

            if (IsWhole(next) || step.Count > 0 || next >= end - Eps) await sink.ClockAsync(at);
            foreach (var c in Diff(room, sent, drifting, final: false)) await sink.StateAsync(c);
            for (var i = 0; i < step.Count; i++)
            {
                var row = step[i];
                rows.Add(row);
                await sink.RowAsync(row);
                if (row.Kind != "fault" || row.Device is not string dev || !room.Faulted.Add(dev)) continue;
                var before = (JsonObject)room.Devices.DeepClone();
                var go = await sink.FirstFaultAsync(dev, row);
                Resync(before, room, sent); // the sink emitted what it changed
                if (go) continue;
                // The step's later rows share the fault's time and their state is already out: send them too.
                foreach (var rest in step.Skip(i + 1))
                {
                    rows.Add(rest);
                    await sink.RowAsync(rest);
                }
                await sink.StepDoneAsync();
                return new(rows, EndReadings(room), Paused: true);
            }
            await sink.StepDoneAsync();

            var wait = Math.Min(cap - spent, _pacing.IdleHourSeconds * (next - prev) + _pacing.EventSeconds * step.Count);
            if (wait > 0)
            {
                spent += wait;
                // A pacer may apply the visitor's edits while it waits (EditServicingPacer); the edit emitted what it
                // changed, so later diffs start from that, not from what this run last sent.
                var before = (JsonObject)room.Devices.DeepClone();
                await _pacer.WaitAsync(TimeSpan.FromTicks((long)Math.Round(wait * TimeSpan.TicksPerSecond)), ct);
                Resync(before, room, sent);
            }
        }

        foreach (var c in Diff(room, sent, drifting, final: true)) await sink.StateAsync(c);
        return new(rows, EndReadings(room), Paused: false);
    }

    /// <summary>The first time after <paramref name="after"/> that the schedule fires, or null if it never will.</summary>
    double? NextFire(Schedule s, double after)
    {
        double? best = s.OnceHour is double once && once > after + Eps ? once : null;
        if (s.DailyAt is string daily)
        {
            var t = TimeOnly.ParseExact(daily, "HH:mm", CultureInfo.InvariantCulture);
            var firstDay = Math.Floor(after / 24);
            for (var d = firstDay; d <= firstDay + 7; d++)
            {
                var h = d * 24 + t.Hour + t.Minute / 60.0;
                if (h <= after + Eps || (s.Days is not null && !s.Days.Contains(StartDay.AddDays(d).DayOfWeek))) continue;
                best = best is double b ? Math.Min(b, h) : h;
                break;
            }
        }
        return best;
    }

    /// <summary>
    /// The state changes the browser hasn't seen, and marks them sent. Booleans, strings and action-set numbers go
    /// out when they change; numbers the scene drifts wait until they moved DriftStep. Final sends every difference.
    /// </summary>
    static List<StateChange> Diff(RoomState room, JsonObject sent, HashSet<string> drifting, bool final)
    {
        var changes = new List<StateChange>();
        foreach (var (dev, node) in room.Devices)
        {
            if (node is not JsonObject o) continue;
            foreach (var (key, v) in o)
            {
                var path = $"{dev}.{key}";
                var old = (sent[dev] as JsonObject)?[key];
                var moved = !final && drifting.Contains(path) && Number(v) is double a && Number(old) is double b
                    ? Math.Abs(a - b) >= DriftStep - Eps
                    : !JsonNode.DeepEquals(v, old);
                if (moved) changes.Add(new(path, v?.DeepClone(), 0));
            }
        }
        foreach (var c in changes) MarkSent(sent, c.Path, c.Value);
        return changes;
    }

    static void Resync(JsonObject before, RoomState room, JsonObject sent)
    {
        foreach (var (dev, node) in room.Devices)
            if (node is JsonObject o)
                foreach (var (key, v) in o)
                    if (!JsonNode.DeepEquals(v, (before[dev] as JsonObject)?[key])) MarkSent(sent, $"{dev}.{key}", v);
    }

    static void MarkSent(JsonObject sent, string path, JsonNode? value)
    {
        var i = path.IndexOf('.');
        var (dev, key) = (path[..i], path[(i + 1)..]);
        if (sent[dev] is not JsonObject o) sent[dev] = o = new JsonObject();
        o[key] = value?.DeepClone();
    }

    static JsonObject EndReadings(RoomState room)
    {
        var end = room.ToWire();
        foreach (var (dev, node) in end.ToList())
        {
            if (room.Failed.Contains(dev)) { end[dev] = new JsonObject { ["error"] = "not responding" }; continue; }
            if (node is not JsonObject o) continue;
            foreach (var (key, v) in o.ToList())
                if (Number(v) is double x) o[key] = Math.Round(x, 2);
        }
        return end;
    }

    static bool IsWhole(double h) => Math.Abs(h - Math.Round(h)) < Eps;

    static double? Number(JsonNode? n) =>
        n is JsonValue v && v.GetValueKind() == JsonValueKind.Number
            ? double.Parse(v.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture)
            : null;
}
