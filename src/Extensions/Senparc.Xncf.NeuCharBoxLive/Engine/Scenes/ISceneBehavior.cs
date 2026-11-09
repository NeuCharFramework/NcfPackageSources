/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ISceneBehavior.cs
    文件功能描述：场景行为接口：动作执行、读数、漂移与状态变化记录


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

public enum Outcome { Confirmed, Commanded, Failed, Reading, MadeSafe, Invalid }

public record ActionCall(string Action, JsonObject Args, string? Step)
{
    public ActionCall Copy() => this with { Args = (JsonObject)Args.DeepClone() };
}

/// <summary>One state write. Ms is the tween length; DelayMs is the wait after the action starts.</summary>
public record StateChange(string Path, JsonNode? Value, int Ms, int DelayMs = 0);

public record ActionOutcome(bool Ok, Outcome Outcome, string Text, IReadOnlyList<StateChange> Changes, int DurationMs, JsonObject ModelResult);

/// <summary>
/// Kind is "action" | "notify" | "fault". Origin is "rule", "schedule" or "held" for a row an automation made, and
/// Automation is then that rule's or schedule's id.
/// </summary>
public record TimelineRow(string At, string Text, bool Ok, string Kind, string? Device, string? Step, string? Origin = null, string? Automation = null);

public record SimHappening(IReadOnlyList<StateChange> Changes, TimelineRow? Row);

public interface ISceneBehavior
{
    ActionOutcome Apply(ActionSpec spec, JsonObject args, RoomState room);
    string Label(ActionSpec spec, JsonObject args);
    IReadOnlyList<SimHappening> Advance(RoomState room, double fromHour, double toHour);
    double? NextHappening(RoomState room);
}
