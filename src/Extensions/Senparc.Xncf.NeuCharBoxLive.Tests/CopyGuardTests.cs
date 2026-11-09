/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CopyGuardTests.cs
    文件功能描述：NeuCharBox Live 测试：CopyGuardTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class CopyGuardTests
{
    [DataTestMethod]
    [DataRow("I set it up over Zigbee.")] [DataRow("Works with Matter")] [DataRow("Thread network")] [DataRow("Z-Wave and zwave")]
    [DataRow("100% reliable")] [DataRow("100 % sure")] [DataRow("I guarantee it")] [DataRow("Home Assistant runs it")]
    public void Find_Hits(string t) => Assert.IsTrue(CopyGuard.Find(t).Count > 0);

    [DataTestMethod]
    [DataRow("Blinds are fully closed.")] [DataRow("Home Assistant bridge is in progress.")] [DataRow("Soil is at 34%.")] [DataRow("Threaded? no: threads.")]
    public void Find_Clean(string t) => Assert.AreEqual(0, CopyGuard.Find(t).Count);

    [TestMethod] public void Strip_RemovesAndTidies() => Assert.AreEqual("I closed the blinds.", CopyGuard.Strip("I closed the blinds 100% ."));
    [TestMethod] public void TwoSentences_Truncates() => Assert.AreEqual("A one. B two.", CopyGuard.TwoSentences("A one. B two. C three."));
    [TestMethod] public void TwoSentences_KeepsShortText() => Assert.AreEqual("Only one. ok", CopyGuard.TwoSentences("Only one. ok"));

    static JsonNode Num(JsonObject p, double v) =>
        (string)p["type"]! == "integer" ? JsonValue.Create((long)Math.Round(v)) : JsonValue.Create(v);

    static IEnumerable<JsonObject> BoundaryArgs(ActionSpec spec)
    {
        var props = spec.Parameters["properties"]!.AsObject();
        if (props.Count == 0) { yield return new JsonObject(); yield break; }
        foreach (var (name, schema) in props)
        {
            var s = schema!.AsObject();
            var type = (string)s["type"]!;
            var values = new List<JsonNode>();
            if (type == "boolean") { values.Add(JsonValue.Create(true)); values.Add(JsonValue.Create(false)); }
            else
            {
                var min = (double)s["minimum"]!; var max = (double)s["maximum"]!;
                foreach (var v in new[] { min, max, (min + max) / 2 }) values.Add(Num(s, v));
            }
            foreach (var v in values)
            {
                var args = new JsonObject();
                foreach (var (n2, s2) in props) // other parameters held at their minimum / true
                    args[n2] = n2 == name ? v.DeepClone()
                        : (string)s2!["type"]! == "boolean" ? JsonValue.Create(true) : Num(s2.AsObject(), (double)s2["minimum"]!);
                yield return args;
            }
        }
    }

    /// <summary>Every scene in ManifestLoader.SceneIds: its labels, action texts, device names and "Try:" lines.</summary>
    [TestMethod]
    public void ServerTexts_PassGuard()
    {
        Assert.IsTrue(ManifestLoader.SceneIds.Count > 0);
        foreach (var id in ManifestLoader.SceneIds)
        {
            var m = ManifestLoader.Load(id);
            var b = ManifestLoader.Behavior(id);
            var checkedCount = 0;
            foreach (var spec in m.Devices.SelectMany(d => d.Actions))
                foreach (var args in BoundaryArgs(spec))
                {
                    var label = b.Label(spec, args);
                    Assert.AreEqual(0, CopyGuard.Find(label).Count, $"{id}: label '{label}' ({spec.Name} {args.ToJsonString()})");
                    var text = b.Apply(spec, args, RoomState.PostSetup(m)).Text;
                    Assert.AreEqual(0, CopyGuard.Find(text).Count, $"{id}: text '{text}' ({spec.Name} {args.ToJsonString()})");
                    checkedCount++;
                }
            var actions = m.Devices.Sum(d => d.Actions.Count);
            Assert.IsTrue(checkedCount >= actions, $"{id}: {checkedCount} cases for {actions} actions");
            if (id == "home") Assert.IsTrue(checkedCount >= 16, $"home: only {checkedCount} cases");
            foreach (var d in m.Devices)
                Assert.AreEqual(0, CopyGuard.Find($"{d.Name} not responding").Count, $"{id}: {d.Name}");
            foreach (var s in m.Suggestions.Append(m.Title))
                Assert.AreEqual(0, CopyGuard.Find(s).Count, $"{id}: '{s}'");
        }
        Assert.AreEqual(0, CopyGuard.Find("not responding").Count);
    }
}
