/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ToolCatalog.cs
    文件功能描述：模型可见的工具声明（规划、执行、总结阶段）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

/// <summary>
/// The tools the model sees, as declarations only: the server runs every call itself, after ArgValidator and PlanGate.
/// Every schema is additionalProperties:false.
/// </summary>
public static class ToolCatalog
{
    internal const int MaxStepText = 120, MaxNotifyText = 200, MaxHours = 168;

    // "HH:MM" every day, or "yyyy-MM-ddTHH:mm" once. [0-9], not \d: .NET's \d also matches other scripts' digits.
    const string AtPattern =
        "^(?:[01][0-9]|2[0-3]):[0-5][0-9]$|^[0-9]{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12][0-9]|3[01])T(?:[01][0-9]|2[0-3]):[0-5][0-9]$";

    /// <summary>Planning: the read actions, submit_plan and cannot_do.</summary>
    public static IList<AITool> PlanTools(SceneManifest m) =>
        Actions(m).Where(a => a.Kind == ActionKind.Read)
            .Select(a => Tool(a.Name, a.Description, (JsonObject)a.Parameters.DeepClone()))
            .Append(Tool("submit_plan",
                "Submit your plan for the visitor to approve. Nothing runs until they do. Each step says in plain words " +
                "what you'll do, and lists every device it changes or reads.",
                SubmitPlanSchema(m)))
            .Append(Tool("cannot_do",
                "Use this instead of a plan when the request can't be done with the devices here. " +
                "Give the reason and 1 to 3 things the visitor could ask instead.",
                CannotDoSchema()))
            .ToList();

    /// <summary>The run: every action with a "step" (required on writes), plus the generic tools.</summary>
    public static IList<AITool> RunTools(SceneManifest m) =>
        Actions(m).Select(a => Tool(a.Name, a.Description, WithStep(a.Parameters, required: a.Kind == ActionKind.Write)))
            .Concat(new[] { Schedule(m), AddRule(m), NotifyUser(), RunUntil(), ProposeReplan(m) })
            .ToList();

    /// <summary>
    /// The paused clock's one turn: the safe-state actions (step optional), notify_user and propose_replan.
    /// A write with a safe value (set_lamp at 0) is offered at that value only.
    /// </summary>
    public static IList<AITool> FaultTurnTools(SceneManifest m) =>
        Actions(m).Where(a => a.Kind == ActionKind.Safe || a.SafeWhen is { Count: > 0 })
            .Select(a => Tool(a.Name, a.Description, WithStep(SafeValuesOnly(a), required: false)))
            .Concat(new[] { NotifyUser(), ProposeReplan(m) })
            .ToList();

    /// <summary>A tool's parameter schema as a JsonObject, to validate its calls against.</summary>
    public static JsonObject SchemaOf(AITool tool) =>
        JsonNode.Parse(((AIFunctionDeclaration)tool).JsonSchema.GetRawText())!.AsObject();

    internal static JsonObject SubmitPlanSchema(SceneManifest m, int maxSteps = 6) => Obj(new()
    {
        ["steps"] = Steps(m, maxSteps),
        ["horizon_hours"] = new JsonObject
        {
            ["type"] = "number", ["minimum"] = 1, ["maximum"] = MaxHours,
            ["description"] = "How many hours the plan covers, when it schedules things or sets rules for later.",
        },
    }, "steps");

    internal static JsonObject ReplanSchema(SceneManifest m, int maxSteps = 6) => Obj(new()
    {
        ["steps"] = Steps(m, maxSteps),
        ["reason"] = new JsonObject { ["type"] = "string", ["minLength"] = 1, ["description"] = "Why the plan needs to change." },
    }, "steps", "reason");

    static IEnumerable<ActionSpec> Actions(SceneManifest m) => m.Devices.SelectMany(d => d.Actions);

    static AITool Tool(string name, string description, JsonObject schema)
    {
        using var doc = JsonDocument.Parse(schema.ToJsonString());
        return AIFunctionFactory.CreateDeclaration(name, description, doc.RootElement.Clone(), null);
    }

    static JsonObject Obj(JsonObject properties, params string[] required) => new()
    {
        ["type"] = "object",
        ["properties"] = properties,
        ["required"] = Strings(required),
        ["additionalProperties"] = false,
    };

    static JsonArray Strings(IEnumerable<string> values) => new(values.Select(v => (JsonNode?)v).ToArray());

    static JsonObject Text(int maxLength, string? description = null)
    {
        var o = new JsonObject { ["type"] = "string", ["minLength"] = 1, ["maxLength"] = maxLength };
        if (description is not null) o["description"] = description;
        return o;
    }

    static JsonObject StepProperty(bool required) => new()
    {
        ["type"] = "string",
        ["description"] = required
            ? "The id of the approved plan step this change belongs to, e.g. \"s1\"."
            : "Optional: the id of the plan step this belongs to. Reads and safe-state calls don't need one.",
    };

    static JsonObject WithStep(JsonObject parameters, bool required)
    {
        var s = (JsonObject)parameters.DeepClone();
        if (s["properties"] is not JsonObject props) s["properties"] = props = new JsonObject();
        props["step"] = StepProperty(required);
        if (required)
        {
            if (s["required"] is not JsonArray r) s["required"] = r = new JsonArray();
            r.Add("step");
        }
        return s;
    }

    static JsonObject SafeValuesOnly(ActionSpec a)
    {
        var s = (JsonObject)a.Parameters.DeepClone();
        foreach (var (key, value) in a.SafeWhen ?? new JsonObject())
            if (s["properties"]?[key] is JsonObject p) p["enum"] = new JsonArray(value?.DeepClone());
        return s;
    }

    static JsonObject Steps(SceneManifest m, int maxSteps) => new()
    {
        ["type"] = "array", ["minItems"] = 1, ["maxItems"] = maxSteps,
        ["items"] = Obj(new()
        {
            ["id"] = new JsonObject { ["type"] = "string", ["description"] = "The step id, e.g. \"s1\"." },
            ["text"] = Text(MaxStepText, "What this step does, in plain words."),
            ["devices"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject { ["type"] = "string", ["enum"] = Strings(m.Devices.Select(d => d.Id)) },
                ["description"] = "Every device this step changes or reads.",
            },
        }, "id", "text", "devices"),
    };

    static JsonObject ActionName(SceneManifest m) => new() { ["type"] = "string", ["enum"] = Strings(Actions(m).Select(a => a.Name)) };

    static JsonObject ActionArgs() => new() { ["type"] = "object", ["description"] = "The action's own arguments." };

    static JsonObject CannotDoSchema() => Obj(new()
    {
        ["reason"] = new JsonObject { ["type"] = "string", ["minLength"] = 1 },
        ["suggestions"] = new JsonObject
        {
            ["type"] = "array", ["minItems"] = 1, ["maxItems"] = 3,
            ["items"] = new JsonObject { ["type"] = "string", ["minLength"] = 1 },
        },
    }, "reason", "suggestions");

    static AITool Schedule(SceneManifest m) => Tool("schedule",
        "Schedule a device action for later. It runs while time passes in run_until.",
        Obj(new()
        {
            ["at"] = new JsonObject
            {
                ["type"] = "string", ["pattern"] = AtPattern,
                ["description"] = "\"HH:MM\" for every day, or \"yyyy-MM-ddTHH:mm\" for once",
            },
            ["days"] = new JsonObject
            {
                ["type"] = "array", ["minItems"] = 1,
                ["items"] = new JsonObject { ["type"] = "string", ["enum"] = Strings(new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }) },
                ["description"] = "Only on these days, with a daily \"HH:MM\" time.",
            },
            ["action"] = ActionName(m),
            ["args"] = ActionArgs(),
            ["step"] = StepProperty(required: true),
        }, "at", "action", "args", "step"));

    static AITool AddRule(SceneManifest m) => Tool("add_rule",
        "Add a rule that runs on its own while time passes: when a device field goes below or above a number, or " +
        "becomes a value, run an action or notify the visitor. Give \"then\" either {action, args} or {notify}.",
        Obj(new()
        {
            ["when"] = Obj(new()
            {
                ["device"] = new JsonObject { ["type"] = "string", ["enum"] = Strings(m.Devices.Select(d => d.Id)) },
                ["field"] = new JsonObject { ["type"] = "string", ["description"] = "A state field of that device, e.g. \"moisture\"." },
                ["op"] = new JsonObject { ["type"] = "string", ["enum"] = Strings(new[] { "below", "above", "becomes" }) },
                ["value"] = new JsonObject { ["type"] = Strings(new[] { "number", "boolean", "string" }) },
            }, "device", "field", "op", "value"),
            ["then"] = Obj(new()
            {
                ["action"] = ActionName(m),
                ["args"] = ActionArgs(),
                ["notify"] = Text(MaxNotifyText, "A message to the visitor."),
            }),
            ["max_per_day"] = new JsonObject { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 6, ["default"] = 2 },
            ["step"] = StepProperty(required: true),
        }, "when", "then", "step"));

    static AITool NotifyUser() => Tool("notify_user", "Send the visitor a short message.",
        Obj(new() { ["text"] = Text(MaxNotifyText), ["step"] = StepProperty(required: false) }, "text"));

    static AITool RunUntil() => Tool("run_until",
        "Fast-forward the simulated clock. Schedules and rules fire, and the room changes as time passes.",
        Obj(new() { ["hours"] = new JsonObject { ["type"] = "number", ["minimum"] = 1, ["maximum"] = MaxHours } }, "hours"));

    static AITool ProposeReplan(SceneManifest m) => Tool("propose_replan",
        "Ask the visitor to approve new or changed steps when you need a device that isn't in the approved steps. " +
        "Reuse a step's id to change that step; new steps get new ids.",
        ReplanSchema(m));
}
