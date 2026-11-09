/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：CustomRoomTests.cs
    文件功能描述：测试访客自建房间（custom 场景）的创建、校验与状态


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.RegularExpressions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

[TestClass]
public class CustomRoomTests
{
    static RoomDevice D(string type, string id) => new(type, id);

    static List<RoomDevice> Twenty()
    {
        var types = new[] { "floor_lamp", "blinds", "soil_sensor", "water_pump", "door_camera", "thermostat" };
        var l = types.Select(t => D(t, t)).ToList();
        for (var i = 0; l.Count < 20; i++)
        {
            var t = types[i % 6];
            l.Add(D(t, $"{t}_{i / 6 + 2}"));
        }
        return l;
    }

    [TestMethod]
    public void Validate_RejectsBadRooms()
    {
        Assert.AreEqual("bad_room", CustomRoom.Validate(null));
        Assert.AreEqual("bad_room", CustomRoom.Validate(new List<RoomDevice>()));
        Assert.AreEqual("bad_room", CustomRoom.Validate(Enumerable.Range(1, 21).Select(i => D("floor_lamp", i == 1 ? "floor_lamp" : $"floor_lamp_{i}")).ToList()));
        Assert.AreEqual("bad_room", CustomRoom.Validate(new[] { D("toaster", "toaster") }));
        Assert.AreEqual("bad_room", CustomRoom.Validate(new[] { D("floor_lamp", "floor_lamp_1") }));
        Assert.AreEqual("bad_room", CustomRoom.Validate(new[] { D("floor_lamp", "floor_lamp_21") }));
        Assert.AreEqual("bad_room", CustomRoom.Validate(new[] { D("floor_lamp", "lamp_2") }));
        Assert.AreEqual("bad_room", CustomRoom.Validate(new[] { D("floor_lamp", "Floor_lamp") }));
        Assert.AreEqual("bad_room", CustomRoom.Validate(new[] { D("floor_lamp", "floor_lamp_02") }));
        Assert.AreEqual("bad_room", CustomRoom.Validate(new[] { D("floor_lamp", "floor_lamp"), D("floor_lamp", "floor_lamp") }));
        Assert.IsNull(CustomRoom.Validate(new[] { D("floor_lamp", "floor_lamp"), D("floor_lamp", "floor_lamp_2") }));
        Assert.IsNull(CustomRoom.Validate(Twenty()));
        Assert.ThrowsException<ArgumentException>(() => CustomRoom.Build(new List<RoomDevice>()));
    }

    [TestMethod]
    public void Build_NamesFollowIds()
    {
        var m = CustomRoom.Build(new[] { D("floor_lamp", "floor_lamp_2"), D("floor_lamp", "floor_lamp") });
        Assert.AreEqual("custom", m.Id);
        Assert.AreEqual(new DateTime(2026, 11, 13), m.StartDay);
        Assert.AreEqual(16.5, m.StartHour);
        Assert.AreEqual("floor_lamp_2", m.Devices[0].Id);
        Assert.AreEqual("Floor lamp 2", m.Devices[0].Name);
        Assert.AreEqual("the floor lamp 2", m.Devices[0].Ref);
        Assert.AreEqual("floor_lamp", m.Devices[0].Type);
        Assert.AreEqual("Floor lamp", m.Devices[1].Name);
        Assert.AreEqual("the floor lamp", m.Devices[1].Ref);
        var a = m.Devices[0].Actions.Single();
        Assert.AreEqual("set_floor_lamp_2", a.Name);
        Assert.AreEqual("floor_lamp_2", a.Device);
        Assert.AreEqual("set", a.Verb);
        StringAssert.Contains(a.Description, "the floor lamp 2");
        Assert.AreEqual("set_floor_lamp", m.Devices[1].Actions.Single().Name);
        Assert.AreEqual("online", (string)m.Devices[0].Initial["status"]!);
        Assert.IsNotNull(m.Action("set_floor_lamp"));
    }

    [TestMethod]
    public void Build_DeepClonesCatalogNodes()
    {
        var m = CustomRoom.Build(new[] { D("floor_lamp", "floor_lamp") });
        var t = Catalog.Load().Find("floor_lamp")!;
        Assert.AreNotSame(t.Initial, m.Devices[0].Initial);
        Assert.AreNotSame(t.Actions[0].Parameters, m.Devices[0].Actions[0].Parameters);
        Assert.IsNull(t.Initial["status"]);
    }

    [TestMethod]
    public void Build_ActionNamesUniqueAndShort()
    {
        var m = CustomRoom.Build(Twenty());
        var names = m.Devices.SelectMany(d => d.Actions).Select(a => a.Name).ToList();
        Assert.AreEqual(names.Count, names.Distinct().Count());
        foreach (var n in names)
        {
            Assert.IsTrue(n.Length <= 64, n);
            Assert.IsTrue(Regex.IsMatch(n, "^[a-z0-9_]+$"), n);
        }
    }

    [TestMethod]
    public void Build_Suggestions()
    {
        var m = CustomRoom.Build(new[] { D("soil_sensor", "soil_sensor"), D("water_pump", "water_pump"), D("floor_lamp", "floor_lamp") });
        CollectionAssert.AreEqual(new[]
        {
            "Water the plant when the soil sensor reads below 30%",
            "Dim the floor lamp to 30% at 19:00",
            "Check each device and tell me what it's doing",
        }, m.Suggestions.ToList());
        var b = CustomRoom.Build(new[] { D("blinds", "blinds") });
        CollectionAssert.AreEqual(new[] { "Check each device and tell me what it's doing" }, b.Suggestions.ToList());
        var two = CustomRoom.Build(new[] { D("floor_lamp", "floor_lamp_3"), D("floor_lamp", "floor_lamp") });
        StringAssert.Contains(two.Suggestions[0], "the floor lamp 3");
    }

    [TestMethod]
    public void Build_ManifestWorksWithToolCatalog()
    {
        var m = CustomRoom.Build(Twenty());
        foreach (var t in ToolCatalog.PlanTools(m).Concat(ToolCatalog.RunTools(m)))
            Assert.IsNotNull(ToolCatalog.SchemaOf(t));
    }
}
