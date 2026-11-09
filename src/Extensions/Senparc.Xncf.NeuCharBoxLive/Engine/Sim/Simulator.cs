/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Simulator.cs
    文件功能描述：NeuCharBox Live 引擎（Sim）：Simulator


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Ai;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

public class Simulator
{
    readonly SceneManifest _m;
    readonly ISceneBehavior _b;

    public Simulator(SceneManifest m, ISceneBehavior b) { _m = m; _b = b; }

    public ISceneBehavior Behavior => _b;
    public SceneManifest Manifest => _m;

    public ActionOutcome Execute(ActionCall call, RoomState room)
    {
        var spec = _m.Action(call.Action);
        if (spec is null) return Invalid($"unknown action '{call.Action}'");
        // Bad args read as Invalid even on a failed device, and change nothing.
        var args = ArgValidator.Validate(spec.Parameters, call.Args);
        if (!args.Ok) return Invalid(args.Error!);
        if (room.Failed.Contains(spec.Device))
            return new(false, Outcome.Failed, "not responding", Array.Empty<StateChange>(), 0,
                new JsonObject { ["ok"] = false, ["commanded"] = false, ["error"] = "not responding" });
        var o = _b.Apply(spec, call.Args, room);
        foreach (var c in o.Changes) room.Set(c.Path, c.Value);
        return o;
    }

    static ActionOutcome Invalid(string why) =>
        new(false, Outcome.Invalid, why, Array.Empty<StateChange>(), 0,
            new JsonObject { ["ok"] = false, ["commanded"] = false, ["error"] = why });
}
