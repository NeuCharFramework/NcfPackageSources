/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CatalogBehavior.cs
    文件功能描述：按设备目录运行自定义房间里每个设备的行为


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

/// <summary>
/// Runs every device of a custom room from the catalog data (see "The catalog format"): effects, labels, readings and
/// drift. A manifest action resolves to its CatalogAction through ActionSpec.Verb and DeviceSpec.Type.
/// </summary>
public sealed class CatalogBehavior : ISceneBehavior
{
    const int MinMs = 600;
    static readonly Regex Placeholder = new(@"\{([^{}]*)\}", RegexOptions.CultureInvariant);

    readonly SceneManifest _m;
    readonly CatalogData _catalog;

    public CatalogBehavior(SceneManifest m) : this(m, Catalog.Load()) { }
    public CatalogBehavior(SceneManifest m, CatalogData catalog) { _m = m; _catalog = catalog; }

    static double? Number(JsonNode? n) =>
        n is JsonValue v && v.GetValueKind() == JsonValueKind.Number ? double.Parse(v.ToJsonString(), CultureInfo.InvariantCulture) : null;

    static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture) is "-0" ? "0" : v.ToString("0.##", CultureInfo.InvariantCulture); // a small negative never reads "-0"

    /// <summary>Numbers compare by value; strings and booleans by their JSON text.</summary>
    static bool Same(JsonNode? a, JsonNode? b)
    {
        if (a is null || b is null) return false;
        if (Number(a) is double x && Number(b) is double y) return Math.Abs(x - y) < 1e-9;
        return a is JsonValue && b is JsonValue && a.ToJsonString() == b.ToJsonString();
    }

    static bool Matches(JsonObject? want, JsonObject have)
    {
        if (want is null) return true;
        foreach (var (k, v) in want)
            if (!Same(have[k], v)) return false;
        return true;
    }

    static JsonObject Result(bool ok, bool commanded, bool? confirmed = null)
    {
        var o = new JsonObject { ["ok"] = ok, ["commanded"] = commanded };
        if (confirmed is bool c) o["confirmed"] = c;
        return o;
    }

    static ActionOutcome Done(Outcome kind, string text, List<StateChange> changes, int ms) =>
        new(true, kind, text, changes, ms, Result(true, true, kind != Outcome.Commanded));

    static ActionOutcome Invalid(string why) =>
        new(false, Outcome.Invalid, why, Array.Empty<StateChange>(), 0, Result(false, false));

    (DeviceSpec Device, CatalogAction Action)? Resolve(ActionSpec spec)
    {
        var d = _m.Device(spec.Device);
        if (d?.Type is null || spec.Verb is null) return null;
        var a = _catalog.Find(d.Type)?.Actions.FirstOrDefault(x => x.Verb == spec.Verb);
        return a is null ? null : (d, a);
    }

    public ActionOutcome Apply(ActionSpec spec, JsonObject args, RoomState room)
    {
        if (Resolve(spec) is not var (device, action)) return Invalid($"unknown action '{spec.Name}'");

        if (action.Kind == ActionKind.Read) return ReadOutcome(device, action, room);

        var changes = new List<StateChange>();
        var pending = new Dictionary<string, JsonNode?>(StringComparer.Ordinal); // so two effects on one field stack
        var duration = MinMs;
        foreach (var e in action.Effects)
        {
            if (e.Arg is not null && args[e.Arg] is null) continue;
            if (e.If is not null && !Matches(e.If, args)) continue;

            JsonNode? value;
            if (e.Arg is not null)
            {
                var raw = args[e.Arg]!;
                value = e.Scale is double s && Number(raw) is double n ? JsonValue.Create(Math.Round(n * s, 6)) : raw.DeepClone();
            }
            else value = e.Value?.DeepClone();

            if (e.Op == "add")
            {
                var cur = Number(pending.TryGetValue(e.Field, out var p) ? p : room.Get($"{device.Id}.{e.Field}")) ?? 0;
                value = JsonValue.Create(cur + (Number(value) ?? 0));
            }
            pending[e.Field] = value;

            var delay = e.DelayMs;
            if (e.AfterArg is not null && Number(args[e.AfterArg]) is double units)
                delay = Math.Max(e.MinMs, (int)(units * e.MsPerUnit));
            changes.Add(new($"{device.Id}.{e.Field}", value, e.Ms, delay));
            duration = Math.Max(duration, delay + e.Ms);
        }

        var safe = action.Kind == ActionKind.Safe
            || (action.SafeWhen is not null && action.SafeWhen.Count > 0 && Matches(action.SafeWhen, args));
        if (safe) return Done(Outcome.MadeSafe, "made safe", changes, duration);
        return action.Outcome == "commanded"
            ? Done(Outcome.Commanded, "commanded", changes, duration)
            : Done(Outcome.Confirmed, "confirmed", changes, duration);
    }

    ActionOutcome ReadOutcome(DeviceSpec device, CatalogAction action, RoomState room)
    {
        var reading = new JsonObject();
        var parts = new List<string>();
        foreach (var r in action.Reads)
        {
            var raw = room.Get($"{device.Id}.{r.Field}");
            JsonNode shown;
            string text;
            if (Number(raw) is double d)
            {
                var rounded = Math.Round(d, Math.Clamp(r.Round, 0, 15), MidpointRounding.AwayFromZero); rounded += 0.0; // -0 becomes 0
                shown = JsonValue.Create(rounded);
                text = Fmt(rounded);
            }
            else
            {
                shown = raw?.DeepClone() ?? JsonValue.Create("unknown");
                text = raw is JsonValue v && v.GetValueKind() == JsonValueKind.String ? (string)v! : shown.ToJsonString();
            }
            reading[r.Field] = shown;
            parts.Add($"{r.Field.Replace('_', ' ')} {text}{r.Unit}");
        }
        var line = string.Join(", ", parts);
        if (CopyGuard.Find(line).Count > 0) line = line.Replace("%", " percent");
        var result = Result(true, false);
        result["reading"] = reading;
        return new(true, Outcome.Reading, line, Array.Empty<StateChange>(), MinMs, result);
    }

    public string Label(ActionSpec spec, JsonObject args)
    {
        if (Resolve(spec) is not var (device, action)) return spec.Name;
        var chosen = action.Label.FirstOrDefault(l => l.When is null || Matches(l.When, args));
        if (chosen is null) return spec.Name;
        return Placeholder.Replace(chosen.Text, m =>
        {
            var key = m.Groups[1].Value;
            if (key == "name") return device.Name;
            var v = args[key];
            if (v is null) return "";
            if (Number(v) is double d) return Fmt(d);
            if (v is JsonValue jv && jv.GetValueKind() == JsonValueKind.String) return (string)jv!;
            return v.ToJsonString();
        });
    }

    public IReadOnlyList<SimHappening> Advance(RoomState room, double fromHour, double toHour)
    {
        var dh = toHour - fromHour;
        var changes = new List<StateChange>();
        foreach (var device in _m.Devices)
        {
            var type = device.Type is null ? null : _catalog.Find(device.Type);
            if (type is null || type.Drift.Count == 0 || room.Devices[device.Id] is not JsonObject fields) continue;
            var now = (JsonObject)fields.DeepClone(); // so later drifts of one device see earlier ones
            foreach (var d in type.Drift)
            {
                if (!Matches(d.While, now) || Number(now[d.Field]) is not double cur) continue;
                double next;
                if (d.Toward is null) next = cur + d.PerHour * dh;
                else
                {
                    var target = Number(d.Toward) ?? (d.Toward is JsonValue tv && tv.TryGetValue<string>(out var f) ? Number(now[f]) : null);
                    if (target is not double t) continue;
                    var step = Math.Abs(d.PerHour) * dh;
                    next = Math.Abs(t - cur) <= step ? t : cur + Math.Sign(t - cur) * step;
                }
                if (d.Min is double lo) next = Math.Max(lo, next);
                if (d.Max is double hi) next = Math.Min(hi, next);
                if (Math.Abs(next - cur) < 1e-12) continue;
                now[d.Field] = JsonValue.Create(next);
                changes.Add(new($"{device.Id}.{d.Field}", JsonValue.Create(next), 0));
            }
        }
        return new List<SimHappening> { new(changes, null) };
    }

    public double? NextHappening(RoomState room) => null;
}
