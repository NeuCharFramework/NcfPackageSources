/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ManifestLoader.cs
    文件功能描述：加载内置场景清单（嵌入资源）并列出可创建会话的场景


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json.Nodes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

public static class ManifestLoader
{
    public static IReadOnlyList<string> SceneIds { get; } = new[] { "home" };

    /// <summary>The scenes a session can be created on: the fixed ones, plus the visitor-built room (not in <see cref="SceneIds"/>, which has a manifest each).</summary>
    public static IReadOnlyList<string> LiveScenes { get; } = ["home", CustomRoom.SceneId];

    static readonly ConcurrentDictionary<string, SceneManifest> Parsed = new(StringComparer.Ordinal);

    /// <summary>
    /// The scene's manifest, parsed once and shared by every session on it. Nothing may change it: its JSON
    /// (parameters, safeWhen, initial) is read-only, and every user deep-clones a node before attaching or changing it
    /// (RoomState.PostSetup, ToolCatalog). Throws KeyNotFoundException for an unknown scene.
    /// </summary>
    public static SceneManifest Load(string sceneId)
    {
        if (!SceneIds.Contains(sceneId)) throw new KeyNotFoundException($"unknown scene '{sceneId}'");
        return Parsed.GetOrAdd(sceneId, Parse);
    }

    static SceneManifest Parse(string sceneId)
    {
        using var s = typeof(ManifestLoader).Assembly.GetManifestResourceStream($"Senparc.Xncf.NeuCharBoxLive.Engine.Scenes.{sceneId}.json")
            ?? throw new KeyNotFoundException($"scene '{sceneId}' has no manifest");
        var root = JsonNode.Parse(s)!.AsObject();
        var devices = root["devices"]!.AsArray().Select(d =>
        {
            var o = d!.AsObject();
            var id = (string)o["id"]!;
            var actions = o["actions"]!.AsArray().Select(a =>
            {
                var ao = a!.AsObject();
                return new ActionSpec((string)ao["name"]!, id, (string)ao["description"]!,
                    Enum.Parse<ActionKind>((string)ao["kind"]!), Materialized(ao["parameters"]!.DeepClone().AsObject()),
                    ao["safeWhen"] is { } safe ? Materialized(safe.DeepClone().AsObject()) : null);
            }).ToList();
            var edits = (o["edit"]?.AsArray() ?? []).Select(e =>
            {
                var eo = e!.AsObject();
                return new EditSpec((string)eo["field"]!, (string)eo["kind"]!, (double?)eo["min"], (double?)eo["max"]);
            }).ToList();
            return new DeviceSpec(id, (string)o["name"]!, (string)o["ref"]!, Materialized(o["initial"]!.DeepClone().AsObject()), actions) { Edits = edits };
        }).ToList();
        return new SceneManifest((string)root["id"]!, (string)root["title"]!,
            DateTime.ParseExact((string)root["startDay"]!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            (double)root["startHour"]!, root["suggestions"]!.AsArray().Select(x => (string)x!).ToList(), devices);
    }

    /// <summary>
    /// Walks the whole tree once, so every lazily built object and array is built now, before sessions on several
    /// threads read the shared manifest at once.
    /// </summary>
    static JsonObject Materialized(JsonObject o)
    {
        Walk(o);
        return o;

        static void Walk(JsonNode? n)
        {
            switch (n)
            {
                case JsonObject obj:
                    foreach (var (_, v) in obj) Walk(v);
                    break;
                case JsonArray arr:
                    foreach (var v in arr) Walk(v);
                    break;
            }
        }
    }

    public static ISceneBehavior Behavior(string sceneId) => sceneId switch
    {
        "home" => new HomeBehavior(),
        _ => throw new KeyNotFoundException($"unknown scene '{sceneId}'"),
    };

    /// <summary>The behaviour of a built manifest: the data-driven one for a custom room, else the scene's own.</summary>
    public static ISceneBehavior Behavior(SceneManifest m) => m.Id == CustomRoom.SceneId ? new CatalogBehavior(m) : Behavior(m.Id);
}
