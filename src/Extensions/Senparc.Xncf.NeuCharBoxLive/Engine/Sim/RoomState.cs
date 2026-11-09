/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RoomState.cs
    文件功能描述：房间状态：设备状态、规则、定时、模拟时钟与创建顺序


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

public class RoomState
{
    public JsonObject Devices { get; private set; } = new();
    public double Hour { get; set; }
    public List<Schedule> Schedules { get; private set; } = new();
    public List<Rule> Rules { get; private set; } = new();
    public HashSet<string> Failed { get; private set; } = new();
    public HashSet<string> Faulted { get; private set; } = new();
    public bool VisitorDone { get; set; }

    /// <summary>The last creation number handed out by <see cref="NextOrder"/>.</summary>
    public long OrderSeq { get; private set; }

    /// <summary>The next creation number for a rule or schedule, so the automations list keeps the order they were made in.</summary>
    public long NextOrder() => ++OrderSeq;

    /// <summary>
    /// What the AI last saw from each sensor: device id → {"reading": …, "at": "Fri 16:30"}. Prompts show this, never
    /// the sensor's own state, so the AI isn't handed a value it never read.
    /// </summary>
    public JsonObject Observed { get; private set; } = new();

    public static RoomState PostSetup(SceneManifest m)
    {
        var r = new RoomState { Hour = m.StartHour };
        foreach (var d in m.Devices) r.Devices[d.Id] = d.Initial.DeepClone();
        return r;
    }

    /// <summary>Path is "device.key". Returns null for an unknown device or key.</summary>
    public JsonNode? Get(string path)
    {
        var (dev, key) = Split(path);
        return Devices[dev] is JsonObject o ? o[key] : null;
    }

    /// <summary>Stores a copy: a JsonNode has one parent, and the same value also goes into event data.</summary>
    public void Set(string path, JsonNode? value)
    {
        var (dev, key) = Split(path);
        if (Devices[dev] is not JsonObject o) Devices[dev] = o = new JsonObject();
        o[key] = value?.DeepClone();
    }

    public RoomState Clone() => new()
    {
        Devices = (JsonObject)Devices.DeepClone(),
        Hour = Hour,
        Schedules = Schedules.Select(s => s.DeepClone()).ToList(),
        Rules = Rules.Select(r => r.DeepClone()).ToList(),
        Failed = new HashSet<string>(Failed),
        Faulted = new HashSet<string>(Faulted),
        VisitorDone = VisitorDone,
        OrderSeq = OrderSeq,
        Observed = (JsonObject)Observed.DeepClone(),
    };

    public JsonObject ToWire()
    {
        var o = (JsonObject)Devices.DeepClone();
        o["env"] = new JsonObject { ["hour"] = Hour };
        return o;
    }

    static (string dev, string key) Split(string path)
    {
        var i = path.IndexOf('.');
        if (i <= 0 || i == path.Length - 1) throw new ArgumentException($"bad state path '{path}'", nameof(path));
        return (path[..i], path[(i + 1)..]);
    }
}
