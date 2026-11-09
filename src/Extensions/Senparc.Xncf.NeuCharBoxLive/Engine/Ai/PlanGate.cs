/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PlanGate.cs
    文件功能描述：计划闸门：设备变更必须属于已批准的步骤，读取与安全状态例外


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

/// <summary>
/// The server side of "nothing runs until you approve" (spec 3.5, 6): a change must name an approved step that lists
/// its device. Reads and safe-state calls need no approval.
/// </summary>
public static class PlanGate
{
    static readonly GateResult Allow = new(true, false, null);
    static readonly GateResult AllowSafe = new(true, true, null);

    static GateResult Refuse(string why) => new(false, false, why);

    /// <summary>A device action call, with its step already split out and its args already validated.</summary>
    public static GateResult Check(ActionCall call, ApprovedPlan plan, SceneManifest m)
    {
        var spec = m.Action(call.Action);
        if (spec is null) return Refuse($"unknown action '{call.Action}'");
        if (spec.Kind == ActionKind.Read) return Allow;
        if (spec.Kind == ActionKind.Safe || IsSafeValue(spec, call.Args)) return AllowSafe;
        var step = call.Step is null ? null : plan.Step(call.Step);
        if (step is null)
        {
            var homes = plan.Steps.Where(s => s.Devices.Contains(spec.Device)).Select(s => s.Id).ToList();
            return Refuse(homes.Count == 0
                ? $"{spec.Device} isn't in the approved plan; call propose_replan to change the plan"
                : $"every change needs the id of an approved step; {spec.Device} is in step{(homes.Count > 1 ? "s" : "")} {string.Join(", ", homes)}");
        }
        if (!step.Devices.Contains(spec.Device)) return Refuse($"{spec.Device} isn't in step {call.Step}; call propose_replan to change the plan");
        return Allow;
    }

    /// <summary>A generic run tool, after its args passed ArgValidator.Validate against the tool's own schema.</summary>
    public static GateResult CheckTool(string tool, JsonObject args, ApprovedPlan plan, SceneManifest m)
    {
        switch (tool)
        {
            case "schedule":
                return CheckInner(args["action"], args["args"], args["step"], plan, m);
            case "add_rule":
            {
                if (CheckWhen(args["when"] as JsonObject, m) is string bad) return Refuse(bad);
                var then = args["then"] as JsonObject;
                bool hasAction = then?.ContainsKey("action") == true, hasNotify = then?.ContainsKey("notify") == true;
                if (hasAction == hasNotify) return Refuse("\"then\" takes either {action, args} or {notify}");
                return hasAction ? CheckInner(then!["action"], then["args"], args["step"], plan, m) : Allow;
            }
            case "notify_user":
            case "run_until":
            case "propose_replan":
                return Allow;
            default:
                return Refuse($"unknown action '{tool}'");
        }
    }

    /// <summary>Validates submit_plan's args and renumbers the steps s1, s2…</summary>
    public static PlanCheck CheckPlan(JsonNode submitArgs, SceneManifest m, int maxSteps = 6)
    {
        var v = ArgValidator.Validate(ToolCatalog.SubmitPlanSchema(m, maxSteps), submitArgs);
        if (!v.Ok) return new(false, null, v.Error);
        var plan = new ApprovedPlan { HorizonHours = ArgValidator.Num(v.Clean!["horizon_hours"]) };
        plan.Steps.AddRange(ReadSteps(v.Clean!).Select((s, i) => s with { Id = $"s{i + 1}" }));
        return new(true, plan, null);
    }

    /// <summary>
    /// Validates propose_replan's args and gives the steps their final ids: an id already in the plan stays, so that
    /// step will be replaced; any other step gets the next free id s{n+1}, s{n+2}… Refuses a merge past maxSteps.
    /// </summary>
    public static ReplanCheck CheckReplan(JsonNode replanArgs, ApprovedPlan current, SceneManifest m, int maxSteps = 6)
    {
        var v = ArgValidator.Validate(ToolCatalog.ReplanSchema(m, maxSteps), replanArgs);
        if (!v.Ok) return new(false, null, v.Error);
        var steps = ReadSteps(v.Clean!);
        var known = current.Steps.Select(s => s.Id).ToHashSet();
        if (steps.Where(s => known.Contains(s.Id)).GroupBy(s => s.Id).FirstOrDefault(g => g.Count() > 1) is { } twice)
            return new(false, null, $"step {twice.Key} is changed twice; send it once");
        var total = current.Steps.Count + steps.Count(s => !known.Contains(s.Id));
        if (total > maxSteps) return new(false, null, $"the plan would have {total} steps; it can have at most {maxSteps}");

        var next = current.Steps.Count;
        var final = new List<PlanStep>();
        foreach (var s in steps)
        {
            if (known.Contains(s.Id)) { final.Add(s); continue; }
            string id;
            do id = $"s{++next}"; while (known.Contains(id));
            final.Add(s with { Id = id });
        }
        return new(true, final, null);
    }

    static bool IsSafeValue(ActionSpec spec, JsonObject args) =>
        spec.SafeWhen is { Count: > 0 } safe
        && safe.All(kv => args.TryGetPropertyValue(kv.Key, out var v) && ArgValidator.Same(kv.Value, v));

    /// <summary>
    /// schedule and add_rule's {action, args}: validated like a direct call, then the same gate. The inner args must
    /// be an object (or left out), as the tool schema says; only a whole call's args may come as a JSON string.
    /// </summary>
    static GateResult CheckInner(JsonNode? action, JsonNode? args, JsonNode? step, ApprovedPlan plan, SceneManifest m)
    {
        var name = ArgValidator.Str(action) ?? "";
        var spec = m.Action(name);
        if (spec is null) return Refuse($"unknown action '{name}'");
        if (args is not null and not JsonObject) return Refuse($"{name}: args must be a JSON object");
        var v = ArgValidator.Validate(spec.Parameters, args);
        if (!v.Ok) return Refuse($"{name}: {v.Error}");
        return Check(new ActionCall(name, v.Clean!, ArgValidator.Str(step)), plan, m);
    }

    /// <summary>
    /// A rule on a field that doesn't exist, a below/above on a non-number, or a becomes whose value is another JSON
    /// kind than the field's (camera.motion becomes "true") would never fire.
    /// </summary>
    static string? CheckWhen(JsonObject? when, SceneManifest m)
    {
        var id = ArgValidator.Str(when?["device"]);
        var device = id is null ? null : m.Device(id);
        if (device is null) return $"unknown device '{id}'";
        var field = ArgValidator.Str(when!["field"]);
        if (field is null || !device.Initial.ContainsKey(field))
            return $"{device.Id} has no field '{field}'; it has {string.Join(", ", device.Initial.Select(kv => kv.Key))}";
        var op = ArgValidator.Str(when["op"]);
        if (op is "below" or "above" && ArgValidator.Num(when["value"]) is null)
            return "'when.value' must be a number for below and above";
        if (op is "becomes" && Kind(device.Initial[field]) is string kind && Kind(when["value"]) != kind)
            return $"'when.value' must be {kind}, like {device.Id}.{field}";
        return null;
    }

    /// <summary>true and false are one kind here.</summary>
    static string? Kind(JsonNode? v) => v?.GetValueKind() switch
    {
        JsonValueKind.True or JsonValueKind.False => "a boolean",
        JsonValueKind.Number => "a number",
        JsonValueKind.String => "a string",
        JsonValueKind.Array => "an array",
        JsonValueKind.Object => "an object",
        _ => null,
    };

    static List<PlanStep> ReadSteps(JsonObject args) =>
        args["steps"]!.AsArray().Select(s => new PlanStep(
            ArgValidator.Str(s!["id"])!,
            ArgValidator.Str(s["text"])!,
            s["devices"]!.AsArray().Select(d => ArgValidator.Str(d)!).Distinct().ToList())).ToList();
}
