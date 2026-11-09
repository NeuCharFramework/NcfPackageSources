/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Rule.cs
    文件功能描述：规则：条件、触发动作、每日上限与开关状态


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

public enum RuleOp { Below, Above, Becomes }

/// <summary>Task 3 gives rules their behaviour. The mutable members are the run-time counters and the visitor's on/off switch.</summary>
public record Rule(string Id, string Step, string Path, RuleOp Op, JsonNode Value, ActionCall? Then, string? Notify, int MaxPerDay)
{
    public double? LastFiredHour { get; set; }
    public Dictionary<int, int> FiredPerDay { get; set; } = new();
    public JsonNode? LastSeen { get; set; }

    /// <summary>The visitor's switch on the rule's card. An off rule never fires; its LastSeen still follows the value.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Creation order among the room's rules and schedules (RoomState.NextOrder), for the automations list.</summary>
    public long Order { get; init; }

    public Rule DeepClone() => this with
    {
        Value = Value.DeepClone(),
        Then = Then?.Copy(),
        LastFiredHour = LastFiredHour,
        FiredPerDay = new Dictionary<int, int>(FiredPerDay),
        LastSeen = LastSeen?.DeepClone(),
        Enabled = Enabled,
        Order = Order,
    };
}
