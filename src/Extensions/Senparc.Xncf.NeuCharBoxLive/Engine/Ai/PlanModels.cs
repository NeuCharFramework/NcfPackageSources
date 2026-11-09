/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PlanModels.cs
    文件功能描述：已批准的计划与步骤（含合并的重新规划）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Events;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

/// <summary>The steps the visitor approved, with ids s1, s2… Approved re-plans are merged in.</summary>
public class ApprovedPlan
{
    public List<PlanStep> Steps { get; init; } = new();
    public double? HorizonHours { get; set; }

    public PlanStep? Step(string id) => Steps.FirstOrDefault(s => s.Id == id);

    public ApprovedPlan Clone() => new() { Steps = new List<PlanStep>(Steps), HorizonHours = HorizonHours };

    /// <summary>
    /// A step whose id is already in the plan replaces that step in place; the rest are appended in order.
    /// The ids are final already: PlanGate.CheckReplan assigns them before the replan event.
    /// </summary>
    public void Merge(IReadOnlyList<PlanStep> replan)
    {
        foreach (var s in replan)
        {
            var i = Steps.FindIndex(x => x.Id == s.Id);
            if (i >= 0) Steps[i] = s;
            else Steps.Add(s);
        }
    }
}

/// <summary>Safe is true when the call puts a device into a safe state, which needs no approval.</summary>
public record GateResult(bool Allowed, bool Safe, string? Error);

public record PlanCheck(bool Ok, ApprovedPlan? Plan, string? Error);

public record ReplanCheck(bool Ok, IReadOnlyList<PlanStep>? Steps, string? Error);
