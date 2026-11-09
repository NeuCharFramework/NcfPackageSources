/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Automations.cs
    文件功能描述：房间的自动化列表：按创建顺序把规则与定时转成访客看到的文字、设备与状态


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

/// <summary>
/// The room's automations as the visitor sees them (spec 2026-11-08 §1, §3): one item per rule and schedule, in the
/// order they were made, with a plain line built from the scene manifest's device names and the scene's action labels.
/// Manifest-driven, so a custom room's catalog devices read the same way as Home's.
/// </summary>
public static class Automations
{
    /// <summary>Rules and schedules one room may hold; the AI's add_rule and schedule are refused past it.</summary>
    public const int MaxPerRoom = 20;

    const double Eps = 1e-9;

    static readonly DayOfWeek[] Week =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

    /// <summary>The field words a rule line uses where the bare field name reads badly; any other field is its name, "_" as a space.</summary>
    static readonly Dictionary<string, string> FieldWords = new(StringComparer.Ordinal)
    {
        ["current"] = "temperature", ["target"] = "set point", ["room"] = "room temperature", ["kwh_today"] = "kWh today",
        ["pm25"] = "fine dust", ["mW"] = "power", ["watts"] = "power", ["x"] = "position", ["probeOut"] = "probe out",
    };

    /// <summary>A switch as a rule's condition reads: "When Floor lamp turns off", "When Door camera detects motion".</summary>
    static readonly Dictionary<string, (string On, string Off)> SwitchWords = new(StringComparer.Ordinal)
    {
        ["on"] = ("turns on", "turns off"), ["off"] = ("turns off", "turns on"),
        ["running"] = ("starts running", "stops running"), ["motion"] = ("detects motion", "stops detecting motion"),
        ["armed"] = ("is armed", "is disarmed"), ["open"] = ("opens", "closes"), ["occupied"] = ("is occupied", "is empty"),
        ["muted"] = ("is muted", "is unmuted"), ["signal"] = ("gets a signal", "loses its signal"), ["locked"] = ("locks", "unlocks"),
        ["ringing"] = ("rings", "stops ringing"), ["alarm"] = ("goes off", "goes quiet"), ["sounding"] = ("starts sounding", "stops sounding"),
        ["leak"] = ("detects a leak", "is dry again"), ["present"] = ("detects someone", "detects no one"),
        ["charging"] = ("starts charging", "stops charging"), ["cleaning"] = ("starts cleaning", "stops cleaning"),
        ["playing"] = ("starts playing", "stops playing"), ["printing"] = ("starts printing", "stops printing"),
        ["holding"] = ("picks something up", "lets go"), ["driving"] = ("starts driving", "stops driving"),
    };

    /// <summary>The units the device cards show (the demo's card definitions), by field.</summary>
    static readonly Dictionary<string, string> Units = new(StringComparer.Ordinal)
    {
        ["moisture"] = "%", ["humidity"] = "%", ["battery"] = "%", ["progress"] = "%",
        ["target"] = " °C", ["current"] = " °C", ["temperature"] = " °C", ["room"] = " °C",
        ["watts"] = " W", ["mW"] = " mW", ["kwh_today"] = " kWh", ["x"] = " mm", ["yaw"] = "°", ["pitch"] = "°", ["pm25"] = " µg/m³",
    };

    /// <summary>Today in sim days, the way the rules count FiredPerDay.</summary>
    public static int Today(RoomState room) => (int)Math.Floor(room.Hour / 24 + Eps);

    /// <summary>A one-off schedule whose time has passed (it ran, or its time went by while it was off): it never runs again.</summary>
    public static bool IsDone(Schedule s, RoomState room) => s.DailyAt is null && s.OnceHour is double once && once <= room.Hour + Eps;

    /// <summary>
    /// The contract's <c>Automation</c> items, in creation order: <c>{ id, kind, text, devices, enabled, firedToday, done }</c>.
    /// An item whose text can't be built (a label that throws) is left out and reported to <paramref name="skipped"/>,
    /// so card text can never fail an edit or a run.
    /// </summary>
    public static JsonArray List(SceneManifest m, ISceneBehavior b, RoomState room, Action<string, Exception>? skipped = null)
    {
        var day = Today(room);
        var items = new List<(long Order, JsonObject Node)>();
        foreach (var r in room.Rules)
            if (Guard(r.Id, skipped, () => Item(r.Id, "rule", Text(m, b, r), Devices(m, r), r.Enabled, r.FiredPerDay.GetValueOrDefault(day), false)) is { } n)
                items.Add((r.Order, n));
        foreach (var s in room.Schedules)
            if (Guard(s.Id, skipped, () => Item(s.Id, "schedule", Text(m, b, s), Devices(m, s), s.Enabled, s.FiredPerDay.GetValueOrDefault(day), IsDone(s, room))) is { } n)
                items.Add((s.Order, n));
        // Stable: rules made with no order number (tests) come first, in their own order.
        return new JsonArray(items.OrderBy(x => x.Order).Select(x => (JsonNode?)x.Node).ToArray());
    }

    static JsonObject? Guard(string id, Action<string, Exception>? skipped, Func<JsonObject> build)
    {
        try { return build(); }
        catch (Exception e)
        {
            skipped?.Invoke(id, e);
            return null;
        }
    }

    /// <summary>The ids in the room, rules and schedules.</summary>
    public static IEnumerable<string> Ids(RoomState room) => room.Rules.Select(r => r.Id).Concat(room.Schedules.Select(s => s.Id));

    /// <summary>
    /// The visitor's switch. False when no rule or schedule has that id. Switching never fires anything by itself. A
    /// "becomes" rule switched back on takes the field's value now as already seen (no fire for a value it already
    /// equals). A below/above rule switched back on fires at the next fast-forward or advance tick that finds its value in
    /// the zone, like one just made (6 h gap and per-day cap as before); a hand edit still fires it only on a move into the
    /// zone. The per-day count stays. A schedule simply runs at its next time.
    /// </summary>
    public static bool SetEnabled(RoomState room, string id, bool enabled)
    {
        if (room.Rules.FirstOrDefault(r => r.Id == id) is { } rule)
        {
            if (rule.Enabled == enabled) return true;
            rule.Enabled = enabled;
            if (enabled && rule.Op == RuleOp.Becomes) rule.LastSeen = room.Get(rule.Path)?.DeepClone();
            return true;
        }
        if (room.Schedules.FirstOrDefault(s => s.Id == id) is { } schedule)
        {
            schedule.Enabled = enabled;
            return true;
        }
        return false;
    }

    static JsonObject Item(string id, string kind, string text, IEnumerable<string> devices, bool enabled, int firedToday, bool done) => new()
    {
        ["id"] = id, ["kind"] = kind, ["text"] = text,
        ["devices"] = new JsonArray(devices.Select(d => (JsonNode?)d).ToArray()),
        ["enabled"] = enabled, ["firedToday"] = firedToday, ["done"] = done,
    };

    /// <summary>The watched device, then the device it acts on (if another).</summary>
    static IEnumerable<string> Devices(SceneManifest m, Rule r)
    {
        var watched = r.Path[..r.Path.IndexOf('.')];
        var acted = r.Then is null ? null : m.Action(r.Then.Action)?.Device;
        return acted is null || acted == watched ? [watched] : [watched, acted];
    }

    static IEnumerable<string> Devices(SceneManifest m, Schedule s) =>
        m.Action(s.Call.Action)?.Device is { } d ? [d] : [];

    /// <summary>"When Soil sensor is below 30% → Water pump: run 10 s", "When Door camera detects motion → notify: …".</summary>
    public static string Text(SceneManifest m, ISceneBehavior b, Rule r)
    {
        var i = r.Path.IndexOf('.');
        var (id, field) = (r.Path[..i], r.Path[(i + 1)..]);
        var device = m.Device(id);
        var then = r.Then is { } call ? RuleAction(Label(m, b, call)) : $"notify: {r.Notify}";
        return Guarded($"When {Condition(device, device?.Name ?? id, field, r.Op, r.Value)} → {then}");
    }

    /// <summary>"Every day at 19:00 → Floor lamp → 35%", "Mon, Wed, Fri at 07:00 → Blinds → open", "Once, Sat 08:00 → Thermostat → 19 °C".</summary>
    public static string Text(SceneManifest m, ISceneBehavior b, Schedule s)
    {
        string when;
        if (s.DailyAt is string daily)
            when = s.Days is { Count: > 0 } days && Week.Any(d => !days.Contains(d))
                ? $"{string.Join(", ", Week.Where(days.Contains).Select(Short))} at {daily}"
                : $"Every day at {daily}";
        else if (s.OnceHour is double once) when = $"Once, {SimClock.At(m.StartDay, once).Label}";
        else when = "Never";
        return Guarded($"{when} → {Label(m, b, s.Call)}");
    }

    /// <summary>
    /// The condition: a switch reads as what it does ("Floor lamp turns off"); a number names its field ("Floor lamp
    /// brightness is above 50%", "Blinds closed is above 50%"), except a sensor's one number, which its unit already names
    /// ("Soil sensor is below 30%"); a number reached is "reaches"; any other value "becomes".
    /// </summary>
    static string Condition(DeviceSpec? d, string name, string field, RuleOp op, JsonNode? value)
    {
        if (op == RuleOp.Becomes && value is JsonValue jv && jv.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
        {
            var on = jv.GetValueKind() == JsonValueKind.True;
            return SwitchWords.TryGetValue(field, out var w)
                ? $"{name} {(on ? w.On : w.Off)}"
                : $"{name} {FieldWord(field)} turns {(on ? "on" : "off")}";
        }
        var subject = d is not null && IsSensorsOnlyNumber(d, field) ? name : $"{name} {FieldWord(field)}";
        var shown = Value(d, field, value);
        return op switch
        {
            RuleOp.Below => $"{subject} is below {shown}",
            RuleOp.Above => $"{subject} is above {shown}",
            _ when Number(value) is not null => $"{subject} reaches {shown}",
            _ when field == "at" => $"{name} reaches {shown}",
            _ => $"{subject} becomes {shown}",
        };
    }

    static string Label(SceneManifest m, ISceneBehavior b, ActionCall call) =>
        m.Action(call.Action) is { } spec ? b.Label(spec, call.Args) : call.Action;

    /// <summary>A rule's action reads "Water pump: run 10 s": the label's first arrow, after the condition's, becomes a colon.</summary>
    static string RuleAction(string label)
    {
        var k = label.IndexOf(" → ", StringComparison.Ordinal);
        return k <= 0 ? label : $"{label[..k]}: {label[(k + 3)..]}";
    }

    static string Short(DayOfWeek d) => CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedDayName(d);

    /// <summary>A sensor (read-only device) whose only number is this field: Soil sensor's moisture, Power meter's mW.</summary>
    static bool IsSensorsOnlyNumber(DeviceSpec d, string field)
    {
        if (!Prompts.IsSensor(d)) return false;
        var numbers = d.Initial.Where(kv => kv.Key != "status" && Number(kv.Value) is not null).Select(kv => kv.Key).ToList();
        return numbers.Count == 1 && numbers[0] == field;
    }

    static string FieldWord(string field) => FieldWords.TryGetValue(field, out var w) ? w : field.Replace('_', ' ');

    /// <summary>A switch reads on/off, a 0–1 card field as a percent, any other number with the card's unit.</summary>
    static string Value(DeviceSpec? d, string field, JsonNode? v)
    {
        switch (v)
        {
            case JsonValue jv when jv.GetValueKind() == JsonValueKind.True: return "on";
            case JsonValue jv when jv.GetValueKind() == JsonValueKind.False: return "off";
            case JsonValue jv when jv.GetValueKind() == JsonValueKind.String: return (string)jv!;
        }
        if (Number(v) is not double x) return v?.ToJsonString() ?? "nothing";
        if (d?.Edits.FirstOrDefault(e => e.Field == field) is { Kind: "number", Min: 0.0, Max: 1.0 }) return $"{Fmt(x * 100)}%";
        return Fmt(x) + Unit(d, field);
    }

    static string Unit(DeviceSpec? d, string field)
    {
        if (field == "speed" && d?.Type == "conveyor") return " m/s";
        if (Units.TryGetValue(field, out var u)) return u;
        // A catalog sensor's own reading unit, for a field the table doesn't know.
        var read = d?.Type is string type ? Catalog.Load().Find(type)?.Actions.SelectMany(a => a.Reads).FirstOrDefault(r => r.Field == field) : null;
        return read?.Unit ?? "";
    }

    /// <summary>Server-made text passes the copy guard: a "100%" threshold reads "100 percent".</summary>
    static string Guarded(string text)
    {
        if (CopyGuard.Find(text).Count == 0) return text;
        text = text.Replace("%", " percent");
        return CopyGuard.Find(text).Count == 0 ? text : CopyGuard.Strip(text);
    }

    static string Fmt(double v)
    {
        var s = Math.Round(v, 2).ToString("0.##", CultureInfo.InvariantCulture);
        return s == "-0" ? "0" : s;
    }

    static double? Number(JsonNode? n) =>
        n is JsonValue v && v.GetValueKind() == JsonValueKind.Number
            ? double.Parse(v.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture)
            : null;
}
