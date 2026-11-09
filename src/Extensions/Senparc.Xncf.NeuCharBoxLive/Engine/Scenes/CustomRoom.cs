/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CustomRoom.cs
    文件功能描述：NeuCharBox Live 引擎（Scenes）：CustomRoom


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

public sealed record RoomDevice(string Type, string Id);

/// <summary>Turns a visitor's device list into a scene manifest built from the catalog.</summary>
public static class CustomRoom
{
    public const string SceneId = "custom";
    public const string Title = "a room the visitor built";
    const int MaxSuggestions = 4;

    /// <summary>Null when the room is valid, else "bad_room". 1 to MaxDevices known types; ids are Type or Type_n (n 2 to 20), unique.</summary>
    public static string? Validate(IReadOnlyList<RoomDevice>? devices)
    {
        var cat = Catalog.Load();
        if (devices is null || devices.Count < 1 || devices.Count > cat.MaxDevices) return "bad_room";
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in devices)
        {
            if (d is null || d.Type is null || d.Id is null || cat.Find(d.Type) is null) return "bad_room";
            if (!ValidId(d.Type, d.Id, cat.MaxDevices) || !seen.Add(d.Id)) return "bad_room";
        }
        return null;
    }

    static bool ValidId(string type, string id, int max)
    {
        if (id == type) return true;
        if (!id.StartsWith(type + "_", StringComparison.Ordinal)) return false;
        var tail = id[(type.Length + 1)..];
        if (tail.Length is < 1 or > 2 || tail[0] == '0' || !tail.All(c => c is >= '0' and <= '9')) return false;
        var n = int.Parse(tail);
        return n >= 2 && n <= max;
    }

    /// <summary>The numbered display name and ref of a device id: n=1 is the catalog's own.</summary>
    public static (string Name, string Ref) NameOf(CatalogType t, string id)
    {
        var n = 1;
        if (id.Length > t.Type.Length + 1 && id.StartsWith(t.Type + "_", StringComparison.Ordinal)
            && int.TryParse(id[(t.Type.Length + 1)..], out var k)) n = k;
        return n == 1 ? (t.Name, t.Ref) : ($"{t.Name} {n}", $"{t.Ref} {n}");
    }

    public static SceneManifest Build(IReadOnlyList<RoomDevice> devices)
    {
        if (Validate(devices) is not null) throw new ArgumentException("bad_room", nameof(devices));
        var cat = Catalog.Load();
        var specs = new List<DeviceSpec>();
        var firstRef = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var d in devices)
        {
            var t = cat.Find(d.Type)!;
            var (name, reference) = NameOf(t, d.Id);
            firstRef.TryAdd(t.Type, reference);
            var initial = (JsonObject)t.Initial.DeepClone();
            initial["status"] = "online";
            var actions = t.Actions.Select(a => new ActionSpec($"{a.Verb}_{d.Id}", d.Id,
                a.Description.Replace("{ref}", reference).Replace("{name}", name), a.Kind,
                Materialized((JsonObject)a.Parameters.DeepClone()),
                a.SafeWhen is null ? null : Materialized((JsonObject)a.SafeWhen.DeepClone())) { Verb = a.Verb }).ToList();
            var edits = t.Edit.Select(e => new EditSpec(e.Field, e.Kind, e.Min, e.Max)).ToList();
            specs.Add(new DeviceSpec(d.Id, name, reference, Materialized(initial), actions) { Type = t.Type, Edits = edits });
        }
        var suggestions = new List<string>();
        foreach (var s in cat.Suggestions)
        {
            if (suggestions.Count >= MaxSuggestions) break;
            if (!s.Needs.All(firstRef.ContainsKey)) continue;
            var text = s.Text;
            foreach (var n in s.Needs) text = text.Replace("{" + n + "}", firstRef[n]);
            suggestions.Add(text);
        }
        return new SceneManifest(SceneId, Title, new DateTime(2026, 11, 13), 16.5, suggestions, specs);
    }

    /// <summary>Walks the tree once so every lazily built node exists before the manifest is shared (see ManifestLoader).</summary>
    static JsonObject Materialized(JsonObject o)
    {
        Walk(o);
        return o;

        static void Walk(JsonNode? n)
        {
            switch (n)
            {
                case JsonObject obj: foreach (var (_, v) in obj) Walk(v); break;
                case JsonArray arr: foreach (var v in arr) Walk(v); break;
            }
        }
    }
}
