/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Ev.cs
    文件功能描述：NeuCharBox Live 引擎（Events）：Ev


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Events;

/// <summary>One builder per contract event. Every JsonNode passed in is DeepCloned: a node has one parent and the same nodes live in the room.</summary>
public static class Ev
{
    /// <summary>The whole room: state, clock, and the automations list (empty when none is given).</summary>
    public static (string kind, JsonObject data) Room(JsonObject state, Clock c, JsonArray? automations = null) =>
        ("room", new JsonObject { ["state"] = state.DeepClone(), ["clock"] = ClockNode(c), ["automations"] = automations?.DeepClone() ?? new JsonArray() });

    /// <summary>The full automations list (Automations.List), in creation order.</summary>
    public static (string kind, JsonObject data) Automations(JsonArray items) =>
        ("automations", new JsonObject { ["items"] = items.DeepClone() });

    public static (string kind, JsonObject data) Planning() => ("planning", new JsonObject());

    public static (string kind, JsonObject data) Reading(string device) =>
        ("reading", new JsonObject { ["device"] = device });

    public static (string kind, JsonObject data) Plan(IReadOnlyList<PlanStep> steps, double? horizonHours) =>
        ("plan", new JsonObject { ["steps"] = StepsNode(steps), ["horizonHours"] = horizonHours });

    public static (string kind, JsonObject data) AwaitingApproval(string kind) =>
        ("awaiting_approval", new JsonObject { ["kind"] = kind });

    public static (string kind, JsonObject data) CannotDo(string reason, IReadOnlyList<string> suggestions) =>
        ("cannot_do", new JsonObject { ["reason"] = reason, ["suggestions"] = Strings(suggestions) });

    public static (string kind, JsonObject data) Declined() => ("declined", new JsonObject());
    public static (string kind, JsonObject data) Expired() => ("expired", new JsonObject());

    public static (string kind, JsonObject data) Step(string id, string status) =>
        ("step", new JsonObject { ["id"] = id, ["status"] = status });

    /// <summary>step is null for a read or safe-state call made without one; the key is still written.</summary>
    public static (string kind, JsonObject data) Call(string? step, string callId, string device, string action, JsonObject args, string label) =>
        ("call", new JsonObject
        {
            ["step"] = step, ["callId"] = callId, ["device"] = device, ["action"] = action,
            ["args"] = args.DeepClone(), ["label"] = label,
        });

    public static (string kind, JsonObject data) State(StateChange s) =>
        ("state", new JsonObject { ["path"] = s.Path, ["value"] = s.Value?.DeepClone(), ["ms"] = s.Ms });

    public static (string kind, JsonObject data) Result(string callId, string? step, string device, bool ok, string outcome, string text) =>
        ("result", new JsonObject
        {
            ["callId"] = callId, ["step"] = step, ["device"] = device, ["ok"] = ok, ["outcome"] = outcome, ["text"] = text,
        });

    public static (string kind, JsonObject data) Fault(string device, string text) =>
        ("fault", new JsonObject { ["device"] = device, ["text"] = text });

    public static (string kind, JsonObject data) ClockEv(Clock c) => ("clock", ClockNode(c));

    public static (string kind, JsonObject data) Timeline(TimelineRow r)
    {
        var d = new JsonObject { ["at"] = r.At, ["text"] = r.Text, ["ok"] = r.Ok, ["kind"] = r.Kind };
        if (r.Origin is not null)
        {
            d["origin"] = r.Origin;
            d["device"] = r.Device;
            if (r.Automation is not null) d["automation"] = r.Automation;
        }
        return ("timeline", d);
    }

    public static (string kind, JsonObject data) FromEdit((string kind, JsonObject data) e)
    {
        e.data["src"] = "edit";
        return e;
    }

    public static (string kind, JsonObject data) Replan(IReadOnlyList<PlanStep> steps, string reason) =>
        ("replan", new JsonObject { ["steps"] = StepsNode(steps), ["reason"] = reason });

    public static (string kind, JsonObject data) Say(string delta) => ("say", new JsonObject { ["delta"] = delta });
    public static (string kind, JsonObject data) Summary(string text) => ("summary", new JsonObject { ["text"] = text });

    public static (string kind, JsonObject data) Done(string outcome, Usage u, int remainingToday) =>
        ("done", new JsonObject
        {
            ["outcome"] = outcome,
            ["usage"] = new JsonObject { ["promptTokens"] = u.PromptTokens, ["completionTokens"] = u.CompletionTokens, ["calls"] = u.Calls },
            ["remainingToday"] = remainingToday,
        });

    public static (string kind, JsonObject data) Limit(string kind) => ("limit", new JsonObject { ["kind"] = kind });

    public static (string kind, JsonObject data) Error(string kind, string message) =>
        ("error", new JsonObject { ["kind"] = kind, ["message"] = message });

    /// <summary>Outcome.Invalid is never emitted: a refused call emits no event.</summary>
    public static string Wire(Outcome o) => o switch
    {
        Outcome.Confirmed => "confirmed",
        Outcome.Commanded => "commanded",
        Outcome.Failed => "failed",
        Outcome.Reading => "reading",
        Outcome.MadeSafe => "made_safe",
        _ => throw new ArgumentOutOfRangeException(nameof(o), o, "this outcome has no wire form"),
    };

    static JsonObject ClockNode(Clock c) => new() { ["iso"] = c.Iso, ["label"] = c.Label, ["hour"] = c.Hour };

    static JsonArray StepsNode(IReadOnlyList<PlanStep> steps)
    {
        var a = new JsonArray();
        foreach (var s in steps)
            a.Add(new JsonObject { ["id"] = s.Id, ["text"] = s.Text, ["devices"] = Strings(s.Devices) });
        return a;
    }

    static JsonArray Strings(IReadOnlyList<string> xs)
    {
        var a = new JsonArray();
        foreach (var x in xs) a.Add(x);
        return a;
    }
}
