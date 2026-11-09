/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Prompts.cs
    文件功能描述：NeuCharBox Live 引擎（Ai）：Prompts


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

/// <summary>
/// Every prompt the runner sends, and the fixed lines the visitor may see. The copy rules are spec section 8.
/// </summary>
public static class Prompts
{
    // ---- Visitor-facing fixed lines. Each passes the copy guard (RunnerTests.FixedTexts_PassGuard). ----

    public const string ModelDown = "The AI service didn't answer.";
    public const string NoPlan = "The AI didn't come back with a plan. Try asking another way.";
    public const string Internal = "Something went wrong on the server.";
    public const string SummaryFallback = "I've finished; the checklist shows what each device reported.";
    public const string NotifyFallback = "I have an update for you.";
    public const string CannotDoFallback = "I can't do that with the devices here.";
    public const string ReplanReasonFallback = "I need to change the plan to carry on.";

    public static IReadOnlyList<string> FixedTexts { get; } =
        new[] { ModelDown, NoPlan, Internal, SummaryFallback, NotifyFallback, CannotDoFallback, ReplanReasonFallback, "Step 1" };

    /// <summary>The timeline row for a schedule or rule a merged re-plan no longer allows. Name is manifest text.</summary>
    public static string RemovedByReplan(string deviceName, string what) => $"{deviceName} {what} removed (not in the new plan)";

    /// <summary>A plan step whose text can't be cleaned: named after its devices, which are manifest text.</summary>
    public static string StepFallback(PlanStep step, SceneManifest m, int n)
    {
        var refs = step.Devices.Select(d => m.Device(d)?.Ref).OfType<string>().ToList();
        return refs.Count == 0 ? $"Step {n}" : $"Use {Join(refs)}";
    }

    // ---- Model-facing. ----

    public const string CallSubmitPlan = "Call submit_plan or cannot_do.";
    public const string Approved = "Approved. Run it.";
    public const string PlanningCantChange = "planning can't change devices; only read sensors now, then call submit_plan or cannot_do";
    public const string FactsHeader = "Facts (JSON):\n";
    public const string ReplanApproved = "approved";
    public const string ReplanDeclined = "declined; carry on without these changes";

    /// <summary>The history turn "Reset room" adds (SessionManager.Reset), so the next request doesn't build on what's gone.</summary>
    public const string ResetUser = "Reset the room.";
    public const string ResetAssistant =
        "I put the room back the way it was right after setup. Earlier changes, schedules and rules are gone.";

    static readonly JsonSerializerOptions Relaxed = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>Compact JSON with apostrophes, arrows and accents left as they are.</summary>
    public static string ToJson(JsonNode node) => node.ToJsonString(Relaxed);

    public static string PlanSystem(SceneManifest scene, RoomState room, Clock clock) => new StringBuilder()
        .AppendLine(Persona(scene))
        .AppendLine()
        .Append(Room(scene, room, clock))
        .AppendLine()
        .AppendLine("Plan the visitor's request. Only read sensors now; never change a device. Call submit_plan, or cannot_do if the room can't do it.")
        .AppendLine("- A plan has at most 6 steps. Each step is one short line in plain words saying what you'll do, and lists every device it changes or reads. Nothing runs until the visitor approves.")
        .AppendLine("- When the request covers a period of time (this weekend, every evening, for a week), set horizon_hours to the hours it covers, at most 168, and plan the schedules or rules it needs.")
        .AppendLine("- When the request is off-topic or needs a device that isn't here, call cannot_do: say plainly what you can't do here, and suggest 1 to 3 things these devices can do.")
        .ToString();

    public static string RunSystem(SceneManifest scene, RoomState room, Clock clock, ApprovedPlan plan)
    {
        var b = new StringBuilder()
            .AppendLine(Persona(scene))
            .AppendLine()
            .Append(Room(scene, room, clock))
            .AppendLine()
            .AppendLine("The visitor approved your plan. Run it now with the tools.")
            .AppendLine("- Every change takes the id of its approved step as \"step\". A change to a device that isn't in that step is refused; if you need it, call propose_replan.")
            .AppendLine("- propose_replan asks the visitor. If they approve, its answer lists the steps with their final ids: use those ids.")
            .AppendLine("- Reads, and putting a device into a safe state (turning it off, stopping it), need no step and no approval.")
            .AppendLine("- Each result says whether the device confirmed the change or it was only commanded."
                + (scene.Id == "home" ? " The pump can't confirm itself: confirm watering by reading the soil sensor after it." : ""))
            .AppendLine("- If a device doesn't respond, don't keep retrying it: make things safe if needed and carry on with the rest.");
        b.AppendLine(plan.HorizonHours is double h
            ? $"- The plan covers {Num(h)} hours: set up its schedules and rules first, then use run_until once, at the end, with hours {Num(h)}."
            : "- The plan covers no period of time, so don't call run_until.");
        return b.AppendLine("- When you're finished, reply exactly DONE.").ToString();
    }

    // ---- The paused clock's one turn (spec 5.3) ----

    /// <summary>The fault turn's refusal of a call that isn't a safe-state call; the examples are the turn's own safe actions.</summary>
    public static string PausedSafeOnly(IEnumerable<ActionSpec> safe)
    {
        var examples = safe.Select(a => a.SafeWhen is { Count: > 0 } when
            ? $"{a.Name} with {string.Join(" and ", when.Select(kv => $"{kv.Key} {kv.Value?.ToJsonString()}"))}"
            : a.Name).ToList();
        return "while the clock is paused, only safe-state calls run without approval (turning a device off or stopping it"
            + (examples.Count > 0 ? $": {string.Join(", ", examples)}" : "") + "); call propose_replan for anything else";
    }

    public const string NoHorizon = "the approved plan covers no period of time, so run_until isn't available; reply DONE when the rest is done";

    public static string HorizonUsed(double horizon) =>
        $"the approved plan covers {Num(horizon)} hours and they have passed; reply DONE when the rest is done";

    public static string HorizonClamped(double horizon, double moved) =>
        $"the approved plan covers {Num(horizon)} hours, so the clock moved {Num(moved)} hours";

    public static string NotWhilePaused(string tool) =>
        $"{tool} isn't available while the clock is paused; you can make a device safe, call notify_user, or call propose_replan";

    public static string FaultTurnSystem(SceneManifest scene, RoomState room, Clock clock, ApprovedPlan plan) => new StringBuilder()
        .AppendLine(Persona(scene))
        .AppendLine()
        .Append(Room(scene, room, clock))
        .AppendLine()
        .AppendLine("The visitor approved your plan, and time is passing in the simulation. A device stopped responding, so the clock is paused for one turn.")
        .AppendLine("- Putting a device into a safe state (turning it off, stopping it) and notify_user need no approval. Anything else needs propose_replan; the clock stays paused until the visitor answers.")
        .AppendLine("- Don't call the device that isn't responding again, and never say it did anything.")
        .Append("- Every step id you use must be one of: ").Append(string.Join(", ", plan.Steps.Select(s => s.Id))).AppendLine(".")
        .ToString();

    /// <summary>The fault row and the timeline so far (compact rows), then the turn's instruction.</summary>
    public static string FaultTurn(TimelineRow fault, JsonArray timeline) => new StringBuilder()
        .AppendLine($"{fault.At}: {fault.Text}.")
        .AppendLine("Timeline so far (JSON):")
        .AppendLine(ToJson(timeline))
        .Append("The clock is paused. You may make devices safe or notify the user without approval, or propose a re-plan. Reply DONE otherwise.")
        .ToString();

    /// <summary>The approved plan, as the assistant message the run starts from.</summary>
    public static string PlanMessage(ApprovedPlan plan)
    {
        var b = new StringBuilder("My plan:\n");
        foreach (var s in plan.Steps) b.AppendLine($"{s.Id}. {s.Text} (devices: {string.Join(", ", s.Devices)})");
        if (plan.HorizonHours is double h) b.AppendLine($"It covers {Num(h)} hours.");
        return b.ToString().TrimEnd();
    }

    /// <summary>"Plan: 1. … 2. …", for the conversation history.</summary>
    public static string PlanLine(ApprovedPlan plan) =>
        "Plan: " + string.Join(" ", plan.Steps.Select((s, i) => $"{i + 1}. {Sentence(s.Text)}"));

    public static string SummarySystem(SceneManifest scene) => Persona(scene);

    public static string SummaryInstruction(JsonObject facts)
    {
        var b = new StringBuilder()
            .AppendLine("Write the summary for the visitor: at most two sentences, speaking as \"I\", plain and specific.")
            .AppendLine("Say what was confirmed and what was only commanded. A call whose outcome is \"commanded\" was sent but not confirmed. For the pump, \"confirmed\": true means a later soil reading rose; false means you can't say the plant got water.")
            .AppendLine("A device whose outcome is \"failed\", or that is in failedDevices, did nothing: never say it did anything.")
            .AppendLine("Don't name protocols, don't promise outcomes, and give no success percentages.")
            .AppendLine("Never name a rule or schedule by an id like rule1 or sched1; say what it does.");
        // A what-if device the run never reached (spec 3.4 step 5).
        if (facts["untouchedFailedDevice"] is JsonValue v && v.TryGetValue<string>(out var untouched))
            b.AppendLine($"Say the plan didn't need {untouched} and that you've flagged it.");
        return b.Append(FactsHeader).Append(ToJson(facts)).ToString();
    }

    /// <summary>The one rephrase call after a copy-guard hit (spec 8).</summary>
    public static string Rephrase(IReadOnlyList<string> texts, IReadOnlyList<string> terms) => new StringBuilder()
        .AppendLine($"Rewrite each text below so it doesn't use {Join(terms.Select(t => $"\"{t}\"").ToList())}.")
        .AppendLine("Keep the meaning and about the same length, keep speaking as \"I\" where the text does, and don't add promises, percentages or protocol names.")
        .AppendLine($"Reply with only a JSON array of {texts.Count} string{(texts.Count == 1 ? "" : "s")}, in the same order.")
        .AppendLine("Texts:")
        .Append(ToJson(new JsonArray(texts.Select(t => (JsonNode?)t).ToArray())))
        .ToString();

    static string Persona(SceneManifest scene) => new StringBuilder()
        .AppendLine($"You are NeuCharBox, a small box that runs the devices in this room ({scene.Title}). You speak as \"I\". Your tone is plain, specific and a bit understated.")
        .AppendLine("Rules for everything you write:")
        .AppendLine("- Don't name wireless protocols or standards (Zigbee, Z-Wave, Matter, Thread and the like).")
        .AppendLine("- Don't promise outcomes: no success percentages, no guarantees, never \"100%\".")
        .AppendLine("- Mention Home Assistant only as a bridge that's in progress, if at all.")
        .Append("- Tell commanded from confirmed. Never say a device did something it didn't confirm, and never say a failed device did anything.")
        .ToString();

    /// <summary>A device with only read actions. The AI knows its value only by reading it.</summary>
    public static bool IsSensor(DeviceSpec d) => d.Actions.Count > 0 && d.Actions.All(a => a.Kind == ActionKind.Read);

    /// <summary>
    /// The devices and the clock. Actuators show their state; a sensor shows only the last reading the AI took
    /// (RoomState.Observed), never the room's own value. Failed devices aren't marked: the AI isn't told in advance (spec 3.4).
    /// </summary>
    static string Room(SceneManifest scene, RoomState room, Clock clock)
    {
        var b = new StringBuilder("Devices and their state now:\n");
        foreach (var d in scene.Devices)
        {
            string state;
            if (IsSensor(d))
                state = room.Observed[d.Id] is JsonObject seen && seen["reading"] is JsonNode reading
                    ? $"last read at {seen["at"]}: {ToJson(reading)}"
                    : "not read yet";
            else
                state = room.Devices[d.Id] is JsonNode n ? ToJson(n) : "{}";
            var actions = string.Join("; ", d.Actions.Select(a => $"{a.Name}: {a.Description}"));
            b.AppendLine($"- {d.Id} ({d.Name}): {state}. Actions: {actions}");
        }
        b.AppendLine("A sensor shows only its last reading, if any; read it to know its value now.");
        b.AppendLine("A brightness or closed value from 0 to 1 is a fraction of the action's 0-100 scale: brightness 0.2 is 20.");
        AppendAutomations(b, scene, room);
        b.AppendLine($"The simulated clock reads {clock.Label} ({clock.Iso}).");
        return b.ToString();
    }

    /// <summary>How many automations a prompt lists at most: the newest ones, with a count of the rest.</summary>
    public const int MaxListedAutomations = 20;

    /// <summary>The tool error for an add_rule or schedule past Automations.MaxPerRoom.</summary>
    public static string AutomationsFull(int max) =>
        $"this room already has {max} automations (rules and schedules), the most it can hold; carry on without this one and tell the visitor (Reset room clears them)";

    /// <summary>
    /// The rules and schedules already in the room, each on, off or done (spec 2026-11-08 §2), so the AI neither
    /// re-creates one nor counts on one that is off. Listed by what they do, never by id: the visitor never sees ids.
    /// Nothing when there are none. A card whose text can't be built is left out (the event path logs it).
    /// </summary>
    static void AppendAutomations(StringBuilder b, SceneManifest scene, RoomState room)
    {
        if (room.Rules.Count + room.Schedules.Count == 0) return;
        List<JsonObject> items;
        try { items = Automations.List(scene, ManifestLoader.Behavior(scene), room).OfType<JsonObject>().ToList(); }
        catch (Exception) { return; } // card text never fails a prompt
        var shown = items.Skip(Math.Max(0, items.Count - MaxListedAutomations)).ToList();
        b.AppendLine("Automations already set up, oldest first (the visitor switches each one on or off on its card; you can't switch, change or remove one):");
        if (items.Count > shown.Count) b.AppendLine($"- ({items.Count - shown.Count} older ones not shown)");
        foreach (var a in shown)
            b.AppendLine($"- {((bool)a["done"]! ? "done" : (bool)a["enabled"]! ? "on" : "off")}: {a["text"]}");
        b.AppendLine("An automation that is off doesn't run; a one-off that is done won't run again.");
        b.AppendLine("Don't set up one that is the same as one listed (same condition or time, same action), on or off. You may set up a changed version (another threshold, time or action), alongside one or to replace it; the visitor can switch the old one off.");
        b.AppendLine("If the visitor wants one that is off, tell them they can switch it back on with its card (with cannot_do if there is nothing else to plan).");
        b.AppendLine("Refer to an automation by what it does, never by an id like rule1 or sched1.");
    }

    static string Sentence(string text)
    {
        var t = text.Trim();
        return t.Length > 0 && ".!?".Contains(t[^1]) ? t : t + ".";
    }

    static string Join(IReadOnlyList<string> xs) =>
        xs.Count <= 1 ? string.Concat(xs) : $"{string.Join(", ", xs.Take(xs.Count - 1))} and {xs[^1]}";

    static string Num(double h) => h.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
