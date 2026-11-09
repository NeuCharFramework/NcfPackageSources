/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveRuntime.cs
    文件功能描述：NeuCharBox Live 引擎（Access）：ILiveRuntime 与每个请求读取的 LiveSnapshot（开关、模型、限额、来源）


    创建标识：Senparc - 20261108

----------------------------------------------------------------*/

using Microsoft.Extensions.AI;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>
/// The daily limits. PerCodeDaily is the limit a new invite code starts with; GlobalDaily caps every code's live
/// requests together; DailyTokenBudget is the token kill switch (spec 4.4). Days are usage days (<see cref="UsageDay"/>).
/// </summary>
public sealed record LiveLimits(int PerCodeDaily, int GlobalDaily, long DailyTokenBudget)
{
    public static LiveLimits Defaults { get; } = new(20, 300, 1_000_000);
}

/// <summary>A model the engine calls: Name goes into ChatOptions.ModelId, the run log and guard calls; Client makes the calls.</summary>
public sealed record LiveModel(string Name, IChatClient Client);

/// <summary>
/// What live runs on right now. A request takes one when it starts and keeps it to its end (what-ifs and re-plans
/// included), so changing the runtime never switches a request's model or limits halfway.
/// </summary>
/// <param name="Plan">The planning model; null when there is none (live is then off).</param>
/// <param name="Run">The model for the run, the fault turns and the summary; null when there is none.</param>
/// <param name="Origins">The browser origins allowed to call /live/api.</param>
/// <param name="OffReason">Why live is off (a <see cref="LiveOff"/> code), or null.</param>
public sealed record LiveSnapshot(LiveLimits Limits, LiveModel? Plan, LiveModel? Run, IReadOnlyList<string> Origins, string? OffReason)
{
    /// <summary>Live is on: no off reason, and both models are there.</summary>
    public bool On => OffReason is null && Plan is not null && Run is not null;

    /// <summary>Off for <paramref name="reason"/>: default limits, no models.</summary>
    public static LiveSnapshot Off(string reason, IReadOnlyList<string> origins) => new(LiveLimits.Defaults, null, null, origins, reason);
}

/// <summary>Where the engine reads the current snapshot. Every read may return a different one; a request reads it once.</summary>
public interface ILiveRuntime
{
    LiveSnapshot Current { get; }
}

/// <summary>The off-reason codes of <see cref="LiveSnapshot.OffReason"/>. Task 7 maps each to resx key "Status.Off.&lt;code&gt;".</summary>
public static class LiveOff
{
    public const string Starting = "starting", Config = "config", NotInstalled = "not_installed", ModuleClosed = "module_closed",
        MultiTenant = "multi_tenant", Disabled = "disabled", NoModel = "no_model", ModelMissing = "model_missing",
        ModelUnsupported = "model_unsupported", Unavailable = "unavailable";
}

/// <summary>A runtime that holds one snapshot; set <see cref="Current"/> to replace it.</summary>
public sealed class StaticLiveRuntime(LiveSnapshot snapshot) : ILiveRuntime
{
    public LiveSnapshot Current { get; set; } = snapshot;
}
