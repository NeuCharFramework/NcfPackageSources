/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：HomeBehavior.cs
    文件功能描述：Home 场景的设备行为与起始时间设定


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

public class HomeBehavior : ISceneBehavior
{
    // The pump's running time is compressed on purpose: a 30 s run shows for 15 s, not a 30 s stall.
    // Change PumpMsPerSecond to 1000 and PumpMinMs as wanted to run in real time.
    public const int PumpMsPerSecond = 500;
    public const int PumpMinMs = 2500;
    const int LampMs = 1200, BlindsMs = 3000, ThermostatMs = 600, CameraMs = 600, ReadMs = 600, StopMs = 600;
    const int WaterDelayMs = 2000, WaterMs = 1500;
    const double SoilPerSecond = 1.2, SoilMax = 60;

    static double Num(JsonObject args, string key) => double.Parse(args[key]!.ToJsonString(), CultureInfo.InvariantCulture);
    static string Fmt(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    static JsonObject Result(bool ok, bool commanded, bool? confirmed = null)
    {
        var o = new JsonObject { ["ok"] = ok, ["commanded"] = commanded };
        if (confirmed is bool c) o["confirmed"] = c;
        return o;
    }

    static ActionOutcome Done(Outcome kind, string text, List<StateChange> changes, int ms) =>
        new(true, kind, text, changes, ms, Result(true, true, kind != Outcome.Commanded));

    public ActionOutcome Apply(ActionSpec spec, JsonObject args, RoomState room)
    {
        switch (spec.Name)
        {
            case "set_lamp":
            {
                var b = Num(args, "brightness");
                var changes = new List<StateChange>();
                if (b > 0) changes.Add(new("lamp.on", JsonValue.Create(true), 0));
                changes.Add(new("lamp.brightness", JsonValue.Create(b / 100.0), LampMs));
                if (b <= 0) changes.Add(new("lamp.on", JsonValue.Create(false), 0, LampMs));
                return b <= 0 ? Done(Outcome.MadeSafe, "made safe", changes, LampMs) : Done(Outcome.Confirmed, "confirmed", changes, LampMs);
            }
            case "set_blinds":
                return Done(Outcome.Confirmed, "confirmed",
                    new() { new("blinds.closed", JsonValue.Create(Num(args, "closed_percent") / 100.0), BlindsMs) }, BlindsMs);
            case "set_thermostat":
                return Done(Outcome.Confirmed, "confirmed",
                    new() { new("thermostat.target", JsonValue.Create(Num(args, "celsius")), ThermostatMs) }, ThermostatMs);
            case "set_door_camera":
                return Done(Outcome.Confirmed, "confirmed",
                    new() { new("camera.armed", JsonValue.Create((bool)args["armed"]!), CameraMs) }, CameraMs);
            case "read_soil":
            {
                var m = Math.Round(Num(new JsonObject { ["m"] = room.Get("soil.moisture")?.DeepClone() }, "m"));
                var result = Result(true, false);
                result["reading"] = new JsonObject { ["moisture"] = m };
                return new(true, Outcome.Reading, $"{Fmt(m)}%", Array.Empty<StateChange>(), ReadMs, result);
            }
            case "run_pump":
            {
                var seconds = Num(args, "seconds");
                var run = Math.Max(PumpMinMs, (int)(seconds * PumpMsPerSecond));
                var now = Num(new JsonObject { ["m"] = room.Get("soil.moisture")?.DeepClone() }, "m");
                var changes = new List<StateChange>
                {
                    new("pump.running", JsonValue.Create(true), 0),
                    new("soil.moisture", JsonValue.Create(Math.Min(SoilMax, now + SoilPerSecond * seconds)), WaterMs, WaterDelayMs),
                    new("pump.running", JsonValue.Create(false), 0, run),
                };
                return Done(Outcome.Commanded, "commanded", changes, run);
            }
            case "stop_pump":
                return Done(Outcome.MadeSafe, "made safe", new() { new("pump.running", JsonValue.Create(false), 0) }, StopMs);
            default:
                return new(false, Outcome.Invalid, $"unknown action '{spec.Name}'", Array.Empty<StateChange>(), 0, Result(false, false));
        }
    }

    public string Label(ActionSpec spec, JsonObject args)
    {
        switch (spec.Name)
        {
            case "set_lamp":
            {
                var b = Num(args, "brightness");
                return b <= 0 ? "Floor lamp → off" : b >= 100 ? "Floor lamp → full" : $"Floor lamp → {Fmt(b)}%";
            }
            case "set_blinds":
            {
                var p = Num(args, "closed_percent");
                return p <= 0 ? "Blinds → open" : p >= 100 ? "Blinds → closed" : $"Blinds → {Fmt(p)}% closed";
            }
            case "read_soil": return "Soil sensor → read";
            case "run_pump": return $"Water pump → run {Fmt(Num(args, "seconds"))} s";
            case "stop_pump": return "Water pump → stop";
            case "set_door_camera": return (bool)args["armed"]! ? "Door camera → armed" : "Door camera → disarmed";
            case "set_thermostat": return $"Thermostat → {Fmt(Num(args, "celsius"))} °C";
            default: return spec.Name;
        }
    }

    // Home dynamics (spec 5.2): soil dries 2 % a day, plus 1 % on a sunny afternoon (13:00–17:00, blinds under half closed).
    const double SoilDryPerHour = 2 / 24.0, SunDryPerHour = 0.25, SunFrom = 13, SunTo = 17;
    const double ThermostatPerHour = 0.5, Week = 168, Eps = 1e-9;

    /// <summary>Tue 14:20 in week one: env.hour 0 is home.json's startDay, Fri 2026-11-13. Motion shows for 1 sim minute.</summary>
    const double VisitorHour = 4 * 24 + 14 + 20 / 60.0, MotionHours = 1 / 60.0;

    public IReadOnlyList<SimHappening> Advance(RoomState room, double fromHour, double toHour)
    {
        var dh = toHour - fromHour;
        var drift = new List<StateChange>();
        if (Reading(room, "soil.moisture") is double soil)
        {
            var dry = SoilDryPerHour * dh;
            if (Reading(room, "blinds.closed") is double closed && closed < 0.5) dry += SunDryPerHour * SunnyHours(fromHour, toHour);
            drift.Add(new("soil.moisture", JsonValue.Create(Math.Max(0, soil - dry)), 0));
        }
        if (Reading(room, "thermostat.current") is double current && Reading(room, "thermostat.target") is double target)
        {
            var max = ThermostatPerHour * dh;
            var next = Math.Abs(target - current) <= max ? target : current + Math.Sign(target - current) * max;
            drift.Add(new("thermostat.current", JsonValue.Create(next), 0));
        }
        var list = new List<SimHappening> { new(drift, null) };

        // The visitor: once, the first Tuesday 14:20 the clock crosses while the camera is armed.
        // A failed camera misses the visitor: no motion on a red card.
        if (room.VisitorDone && Flag(room, "camera.motion"))
        {
            if (NextWeekly(fromHour, VisitorHour + MotionHours) <= toHour + Eps)
                list.Add(new(new[] { new StateChange("camera.motion", JsonValue.Create(false), 0) }, null));
        }
        else if (!room.VisitorDone && Flag(room, "camera.armed") && NextWeekly(fromHour, VisitorHour) <= toHour + Eps)
        {
            room.VisitorDone = true;
            if (!room.Failed.Contains("camera"))
                list.Add(new(new[] { new StateChange("camera.motion", JsonValue.Create(true), 0) }, null));
        }
        return list;
    }

    public double? NextHappening(RoomState room)
    {
        if (room.VisitorDone) return Flag(room, "camera.motion") ? NextWeekly(room.Hour, VisitorHour + MotionHours) : null;
        return NextWeekly(room.Hour, VisitorHour);
    }

    /// <summary>The first <paramref name="baseHour"/> + 168·k strictly after <paramref name="after"/>.</summary>
    static double NextWeekly(double after, double baseHour) => baseHour + (Math.Floor((after + Eps - baseHour) / Week) + 1) * Week;

    /// <summary>Hours of [from, to) inside a day's sunny window.</summary>
    static double SunnyHours(double from, double to)
    {
        double sum = 0;
        for (var d = Math.Floor(from / 24); d * 24 < to; d++)
            sum += Math.Max(0, Math.Min(to, d * 24 + SunTo) - Math.Max(from, d * 24 + SunFrom));
        return sum;
    }

    static double? Reading(RoomState room, string path) =>
        room.Get(path) is JsonValue v && v.GetValueKind() == System.Text.Json.JsonValueKind.Number
            ? double.Parse(v.ToJsonString(), CultureInfo.InvariantCulture)
            : null;

    static bool Flag(RoomState room, string path) => room.Get(path) is JsonValue v && v.GetValueKind() == System.Text.Json.JsonValueKind.True;
}
