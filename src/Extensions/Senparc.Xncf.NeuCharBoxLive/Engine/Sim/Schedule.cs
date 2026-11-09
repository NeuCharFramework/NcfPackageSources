/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Schedule.cs
    文件功能描述：NeuCharBox Live 引擎（Sim）：Schedule


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

/// <summary>DailyAt is "HH:MM". OnceHour is an absolute env.hour. Task 3 gives schedules their behaviour.</summary>
public record Schedule(string Id, string Step, ActionCall Call, string? DailyAt, IReadOnlySet<DayOfWeek>? Days, double? OnceHour)
{
    /// <summary>The visitor's switch on the schedule's card. An off schedule never runs; switched back on, it runs at its next time.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Runs per sim day (day = floor(env.hour / 24)), for the card's "ran N× today".</summary>
    public Dictionary<int, int> FiredPerDay { get; set; } = new();

    /// <summary>Creation order among the room's rules and schedules (RoomState.NextOrder), for the automations list.</summary>
    public long Order { get; init; }

    public Schedule DeepClone() => this with
    {
        Call = Call.Copy(),
        Days = Days is null ? null : new HashSet<DayOfWeek>(Days),
        Enabled = Enabled,
        FiredPerDay = new Dictionary<int, int>(FiredPerDay),
        Order = Order,
    };
}
