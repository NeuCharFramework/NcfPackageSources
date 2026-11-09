/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RoomEditor.cs
    文件功能描述：校验并应用访客对设备字段的手动编辑，不调用模型


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

/// <summary>
/// What one hand edit did. Error is null or "device_unavailable". Queued is the answer for an edit held back
/// until the running request finishes: nothing applied yet.
/// </summary>
public record EditOutcome(string? Error, IReadOnlyList<StateChange> Changes, IReadOnlyList<TimelineRow> Rows)
{
    public static EditOutcome Queued { get; } = new(null, [], []);
}

/// <summary>Validates and applies a visitor's hand edit of one device field. Never calls the model.</summary>
public sealed class RoomEditor(SceneManifest m, Simulator sim)
{
    readonly RuleEngine _rules = new(sim);
    // Home's pump and lamp fields have their own handling; every other scene's edit (a custom room's) is a plain set.
    readonly bool _home = m.Id == "home";

    /// <summary>How long a hand-switched pump runs.</summary>
    public const int HandPumpSeconds = 6;

    /// <summary>Null when the edit is allowed, else "bad_value". Pure: reads only the manifest.</summary>
    public static string? Validate(SceneManifest m, string? path, JsonNode? value)
    {
        const string Bad = "bad_value";
        if (path is null || value is not JsonValue v) return Bad;
        var i = path.IndexOf('.');
        if (i <= 0 || i == path.Length - 1) return Bad;
        var spec = m.Device(path[..i])?.Edits.FirstOrDefault(e => e.Field == path[(i + 1)..]);
        if (spec is null) return Bad;
        switch (spec.Kind)
        {
            case "bool":
                return v.GetValueKind() is JsonValueKind.True or JsonValueKind.False ? null : Bad;
            case "number":
                if (v.GetValueKind() != JsonValueKind.Number || !TryNumber(v, out var d)) return Bad;
                return (spec.Min is double lo && d < lo) || (spec.Max is double hi && d > hi) ? Bad : null;
            default:
                return Bad;
        }
    }

    /// <summary>Applies an edit that already passed Validate. A faulted or busy device is left unchanged.</summary>
    public EditOutcome Apply(RoomState room, string path, JsonNode value)
    {
        var device = path[..path.IndexOf('.')];
        if (room.Get($"{device}.status")?.GetValue<string>() is "fault" or "busy")
            return new("device_unavailable", [], []);

        var changes = new List<StateChange>();
        var at = SimClock.At(m.StartDay, room.Hour).Label;
        var day = (int)Math.Floor(room.Hour / 24 + 1e-9);
        var before = room.Rules.Select(r => room.Get(r.Path)?.DeepClone()).ToList();
        if (_home && path == "pump.running")
        {
            var call = value.GetValue<bool>()
                ? new ActionCall("run_pump", new JsonObject { ["seconds"] = HandPumpSeconds }, null)
                : new ActionCall("stop_pump", new JsonObject(), null);
            var run = sim.Execute(call, room);
            if (!run.Ok) return new("device_unavailable", [], []);
            changes.AddRange(run.Changes);
        }
        else
        {
            Set(room, changes, path, value);
            if (_home && path == "lamp.brightness") Set(room, changes, "lamp.on", JsonValue.Create(TryNumber((JsonValue)value, out var b) && b > 0));
        }

        // Each rule is checked once; what a fire changes does not trigger other rules in this edit.
        var rows = new List<TimelineRow>();
        var rules = room.Rules.ToList();
        // A value the edit set counts as seen even if the action then moves it on (a hand pump-on ends with running false).
        var after = rules.Select(r => (changes.FirstOrDefault(c => c.Path == r.Path) is { } c ? c.Value : room.Get(r.Path))?.DeepClone()).ToList();
        for (var i = 0; i < rules.Count; i++)
            if (_rules.AfterEdit(rules[i], before[i], after[i], at, day, room) is { } fired)
            {
                rows.Add(fired.Row);
                changes.AddRange(fired.Changes);
            }
        return new(null, changes, rows);
    }

    // A JSON number may be backed by an int, long or double, and GetValue<double> throws for the first two.
    static bool TryNumber(JsonValue v, out double d) =>
        double.TryParse(v.ToJsonString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d)
        && double.IsFinite(d);

    static void Set(RoomState room, List<StateChange> changes, string path, JsonNode value)
    {
        room.Set(path, value);
        changes.Add(new(path, value.DeepClone(), 0));
    }
}
