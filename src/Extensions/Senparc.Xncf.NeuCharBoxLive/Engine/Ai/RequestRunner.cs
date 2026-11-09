/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RequestRunner.cs
    文件功能描述：NeuCharBox Live 引擎（Ai）：RequestRunner


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Ai;

/// <summary>A model call failed or stalled (after Senparc.Xncf.NeuCharBoxLive.NeuChar's retries). Message is the visitor's line.</summary>
public class ModelCallException(string visitorMessage, Exception? inner) : Exception(visitorMessage, inner);

/// <summary>Today's token kill switch is tripped.</summary>
public class TokenBudgetSpentException() : Exception("today's token budget is spent");

/// <summary>
/// The AI loop (spec 6): plan with read-only tools, wait for approval, run the tool loop through the plan gate, then
/// stream a two-sentence summary. Event order is the contract's. Every model call is bounded by the request budget
/// and cancelled when the work time runs out, so a stalled call or stream can't hold a request past it.
/// </summary>
public sealed class RequestRunner
{
    public const int SummaryMaxTokens = 200;
    public static readonly EventId CopyGuardHit = new(7001, nameof(CopyGuardHit));
    static readonly EventId ModelFailed = new(7002, "ModelCallFailed");
    static readonly EventId RunFailed = new(7003, "RunFailed");
    static readonly EventId StoreFailed = new(7004, "RunStoreFailed");
    static readonly EventId RephraseSkipped = new(7005, "RephraseSkipped");

    /// <summary>A commanded-only action and the read that can confirm it: the pump only through the soil rising (spec 6).</summary>
    static readonly Dictionary<string, (string Read, string Path)> ConfirmedBy = new() { ["run_pump"] = ("read_soil", "soil.moisture") };

    readonly Func<string, IChatClient> _chatFor;
    readonly LiveOptions _opt;
    readonly IPacer _pacer;
    readonly TimeProvider _tp;
    readonly IRunStore _runs;
    readonly ITokenMeter _meter;
    readonly ILogger<RequestRunner> _log;

    /// <param name="chatFor">The client for a model id. Planning uses PlanModel; the run and the summary use RunModel.</param>
    public RequestRunner(Func<string, IChatClient> chatFor, LiveOptions opt, IPacer pacer, TimeProvider tp, IRunStore runs,
        ITokenMeter meter, ILogger<RequestRunner> log)
    {
        _chatFor = chatFor;
        _opt = opt;
        _pacer = pacer;
        _tp = tp;
        _runs = runs;
        _meter = meter;
        _log = log;
    }

    /// <summary>Runs one request to its done event. It never throws for the request's own failures: they end in done.</summary>
    public Task RunAsync(LiveSession s, RequestRecord r, CancellationToken ct) =>
        new Run(this, s, r, _opt.MaxCalls).ExecuteAsync((run, c) => run.NormalAsync(c), ct);

    /// <summary>
    /// "What if something fails?" (spec 3.4): the room goes back to just before <paramref name="prior"/>, the device is
    /// set to fail, and prior's approved plan replays without asking again, with up to MaxCallsWhatIf model calls.
    /// A what-if adds no turn and never becomes LastFinished; the runner logs its run (WhatIf, so it doesn't count).
    /// A prior with no approved plan (declined, expired, cannot_do) or an unknown device is refused: nothing replays,
    /// the room is left as it is, and the request ends error {internal} then done {failed}. Task 9 refuses both first.
    /// </summary>
    public Task RunWhatIfAsync(LiveSession s, RequestRecord prior, string device, RequestRecord r, CancellationToken ct)
    {
        r.WhatIfDevice = device;
        return new Run(this, s, r, _opt.MaxCallsWhatIf, prior.Text).ExecuteAsync((run, c) => run.WhatIfAsync(prior, device, c), ct);
    }

    /// <summary>A re-plan card got no answer in time: the request ends expired.</summary>
    sealed class ReplanExpiredException() : Exception("the re-plan got no answer in time");

    /// <summary>One request's state while it runs.</summary>
    sealed class Run
    {
        readonly RequestRunner _o;
        readonly LiveOptions _opt;
        readonly LiveSession _s;
        readonly RequestRecord _r;
        readonly string _text;
        readonly SceneManifest _m;
        readonly ISceneBehavior _b;
        readonly Simulator _sim;
        readonly RoomEditor _editor;
        readonly RequestBudget _budget;
        readonly long _startTs;
        readonly Dictionary<string, JsonObject> _runSchemas;
        readonly IList<AITool> _faultTools;
        readonly Dictionary<string, JsonObject> _faultSchemas;
        readonly List<JsonObject> _facts = new();
        readonly List<Pending> _unconfirmed = new();
        readonly List<TimelineRow> _timeline = new();
        readonly HashSet<string> _added = new(StringComparer.Ordinal); // schedule and rule ids this run added
        RoomState _room;
        StepTracker? _tracker;
        ApprovedPlan? _plan;
        bool _approved;
        string? _summary;
        Turn? _turn;
        int _callSeq;
        bool _fastForwardFault;
        double _fastForwarded; // hours run_until moved the clock in this run

        /// <summary>A commanded call waiting for a read that shows it worked.</summary>
        sealed record Pending(JsonObject Fact, string Read, string Path, double Baseline);

        /// <param name="text">The visitor's request; a what-if passes the replayed request's text.</param>
        public Run(RequestRunner o, LiveSession s, RequestRecord r, int maxCalls, string? text = null)
        {
            _o = o;
            _opt = o._opt;
            _s = s;
            _r = r;
            _text = text ?? r.Text;
            _m = s.Scene;
            _b = ManifestLoader.Behavior(_m);
            _sim = new Simulator(_m, _b);
            _editor = new RoomEditor(_m, _sim);
            _budget = new RequestBudget(maxCalls, _opt.MaxWork, o._tp);
            _startTs = o._tp.GetTimestamp();
            _room = s.Room;
            _runSchemas = ToolCatalog.RunTools(_m).ToDictionary(t => t.Name, ToolCatalog.SchemaOf);
            _faultTools = ToolCatalog.FaultTurnTools(_m);
            _faultSchemas = _faultTools.ToDictionary(t => t.Name, ToolCatalog.SchemaOf);
        }

        DateTimeOffset Now => _o._tp.GetUtcNow();
        Clock Clock() => SimClock.At(_m.StartDay, _room.Hour);

        // ---- the request's frame: begin, bounds and errors, done ----

        public async Task ExecuteAsync(Func<Run, CancellationToken, Task<string>> body, CancellationToken ct)
        {
            await _s.Gate.WaitAsync(CancellationToken.None);
            try
            {
                _s.Begin(_r);
                _s.LastActivity = Now;
                _room = _s.Room;
            }
            finally { _s.Gate.Release(); }

            string outcome;
            try
            {
                _r.RunId ??= await _o._runs.StartAsync(new RunStart(_s.Code.Code, _m.Id, _text, _r.IsWhatIf, _r.WhatIfDevice,
                    _opt.RunModel, UsageDay.For(Now), Now));
                outcome = await body(this, ct);
            }
            catch (BudgetExceededException e)
            {
                Stop();
                Emit(Ev.Limit(e.Kind));
                outcome = "limit";
            }
            catch (TokenBudgetSpentException)
            {
                Stop();
                Emit(Ev.Limit("budget"));
                outcome = "limit";
            }
            catch (ReplanExpiredException)
            {
                _turn ??= TurnWith("No answer to the change of plan in time; the run stopped.");
                Stop();
                Emit(Ev.Expired());
                outcome = "expired";
            }
            catch (ModelCallException e)
            {
                _o._log.LogWarning(ModelFailed, e.InnerException, "Model call failed in request {Rid}", _r.Rid);
                Stop();
                Emit(Ev.Error("model", e.Message));
                outcome = "failed";
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Stop(); // the session ended; nobody is listening, but the run is still logged
                outcome = "failed";
            }
            catch (Exception e)
            {
                _o._log.LogError(RunFailed, e, "Request {Rid} failed", _r.Rid);
                Stop();
                Emit(Ev.Error("internal", Prompts.Internal));
                outcome = "failed";
            }
            await FinishAsync(outcome);
        }

        /// <summary>A request that ends early closes its checklist; an approved one leaves a turn saying it stopped.</summary>
        void Stop()
        {
            _tracker?.Finish();
            if (_approved) _turn ??= TurnWith("The run stopped before the end.");
        }

        async Task FinishAsync(string outcome)
        {
            if (_r.IsWhatIf) FlagUntouched(); // ended early (limit, expired, error): the card still turns red before done
            var usage = _budget.Usage;
            _r.Outcome = outcome;
            _r.Usage = usage;
            var remaining = await RemainingTodayAsync();
            await _s.Gate.WaitAsync(CancellationToken.None);
            try
            {
                // An edit made after the last wait (the summary stream isn't one) is applied now, before done; any
                // later one finds the session idle and applies itself.
                ServiceEdits();
                if (_turn is not null && !_r.IsWhatIf) _s.AddTurn(_turn, _opt.HistoryTurns);
                _s.End(_r);
                _s.LastActivity = Now;
                Emit(Ev.Done(outcome, usage, remaining));
            }
            finally { _s.Gate.Release(); }

            if (_r.RunId is not long id) return;
            try
            {
                var plan = _plan is null ? null : Prompts.ToJson(Ev.Plan(_plan.Steps, _plan.HorizonHours).data);
                await _o._runs.FinishAsync(id, new RunEnd(outcome, plan, _summary, usage.Calls, usage.PromptTokens,
                    usage.CompletionTokens, (long)_o._tp.GetElapsedTime(_startTs).TotalMilliseconds, Now));
            }
            catch (Exception e)
            {
                _o._log.LogError(StoreFailed, e, "Couldn't log the end of run {RunId}", id);
            }
        }

        async Task<int> RemainingTodayAsync()
        {
            try
            {
                var day = UsageDay.For(Now);
                var mine = await _o._runs.CountTodayAsync(day, _s.Code.Code);
                var all = await _o._runs.CountTodayAsync(day, null);
                return Math.Max(0, Math.Min(_s.Code.DailyLimit - mine, _opt.GlobalDaily - all));
            }
            catch (Exception e)
            {
                _o._log.LogError(StoreFailed, e, "Couldn't count today's runs");
                return 0;
            }
        }

        // ---- a normal request ----

        public async Task<string> NormalAsync(CancellationToken ct)
        {
            ClearWhatIfFault();
            Emit(Ev.Planning());
            var plan = await PlanAsync(ct);
            if (plan is null) return "cannot_do";
            _plan = plan;
            Emit(Ev.Plan(plan.Steps, plan.HorizonHours));
            var d = await AskAsync("plan", ct);
            if (d is null)
            {
                Emit(Ev.Expired());
                _turn = TurnWith("No answer in time; nothing ran.");
                return "expired";
            }
            if (d == Decision.Decline)
            {
                Emit(Ev.Declined());
                _turn = TurnWith("Not this; nothing ran.");
                return "declined";
            }
            _approved = true;
            _r.Plan = plan; // only an approved plan can be replayed by a what-if
            await RunPhaseAsync(plan, ct);
            return await SummaryAsync(ct);
        }

        Turn TurnWith(string result) => new(_text, $"{Prompts.PlanLine(_plan!)} Result: {result}");

        /// <summary>
        /// A what-if is a replay (spec 3.4): its failed device doesn't carry into the next request. Its devices go back
        /// online in the room and in this request's Before, with a state event each.
        /// </summary>
        void ClearWhatIfFault()
        {
            var devices = _room.Failed.Union(_room.Faulted).Order(StringComparer.Ordinal).ToList();
            if (devices.Count == 0) return;
            foreach (var room in new[] { _room, _r.Before }) Online(room, devices);
            foreach (var d in devices) EmitState(new StateChange($"{d}.status", JsonValue.Create("online"), 0));
        }

        static void Online(RoomState room, IEnumerable<string> devices)
        {
            foreach (var d in devices) room.Set($"{d}.status", JsonValue.Create("online"));
            room.Failed.Clear();
            room.Faulted.Clear();
        }

        // ---- a what-if ----

        /// <summary>room, then plan with no awaiting_approval, then the run (contract, what-if requests).</summary>
        public async Task<string> WhatIfAsync(RequestRecord prior, string device, CancellationToken ct)
        {
            if (prior.Plan is null) throw new InvalidOperationException($"request {prior.Rid} has no approved plan to replay");
            if (_m.Device(device) is null) throw new InvalidOperationException($"unknown device '{device}'");
            var room = prior.Before.Clone(); // devices, clock, and rules and schedules with their counters
            Online(room, room.Failed.Union(room.Faulted).ToList());
            room.Failed.Add(device); // the AI isn't told: it finds out at the first failing call
            await _s.Gate.WaitAsync(CancellationToken.None);
            try { _s.Room = room; }
            finally { _s.Gate.Release(); }
            _room = room;
            // Every older edit's delayed frame belongs to the room this one replaces: dropped, in one step with the room
            // event. The replayed room's automations, with their on/off state, go in the room event and as the list.
            var automations = EditApplier.ListOf(_s, room);
            var ids = Automations.Ids(room).ToList();
            lock (_s.EmitGate)
            {
                _s.InvalidateDelayedEdits();
                Emit(Ev.Room(room.ToWire(), Clock(), automations ?? new JsonArray()));
                if (automations is not null) EditApplier.AppendAutomations(_s, _r.Rid, automations, ids, edit: false, force: true);
            }

            // A clone: a re-plan approved in the replay changes this run only, so prior replays the same plan again.
            var plan = prior.Plan.Clone();
            _plan = plan;
            _r.Plan = plan;
            Emit(Ev.Plan(plan.Steps, plan.HorizonHours));
            _approved = true;
            await RunPhaseAsync(plan, ct);
            return await SummaryAsync(ct);
        }

        /// <summary>
        /// Opens the card, then emits awaiting_approval, so an answer sent right after the event finds the card
        /// waiting. The work clock is paused meanwhile. Edits made meanwhile apply at once and don't change the plan.
        /// </summary>
        async Task<Decision?> AskAsync(string kind, CancellationToken ct)
        {
            using (_budget.PauseForApproval())
            {
                var wait = _r.WaitDecisionAsync(_opt.ApprovalWait, ct);
                Emit(Ev.AwaitingApproval(kind));
                return await WhileServicing(wait);
            }
        }

        // ---- 1. plan ----

        /// <summary>The checked, guarded plan; null when the model called cannot_do (already emitted).</summary>
        async Task<ApprovedPlan?> PlanAsync(CancellationToken ct)
        {
            var client = _o._chatFor(_opt.PlanModel);
            var tools = ToolCatalog.PlanTools(_m);
            var cannotDo = ToolCatalog.SchemaOf(tools.Single(t => t.Name == "cannot_do"));
            var msgs = new List<ChatMessage> { new(ChatRole.System, Prompts.PlanSystem(_m, _room, Clock())) };
            AddHistory(msgs);
            msgs.Add(new ChatMessage(ChatRole.User, _text));
            var options = new ChatOptions
            {
                ModelId = _opt.PlanModel,
                Tools = tools,
                ToolMode = string.Equals(_opt.PlanToolMode, "auto", StringComparison.OrdinalIgnoreCase) ? ChatToolMode.Auto : ChatToolMode.RequireAny,
                MaxOutputTokens = _opt.MaxCompletionTokens,
            };
            var nudged = false;
            while (true)
            {
                var resp = await CallAsync(client, msgs, options, ct);
                msgs.AddRange(resp.Messages);
                var calls = CallsIn(resp);
                if (calls.Count == 0)
                {
                    // The plain-JSON fallback (spec 6): a text answer holding {"steps": …}. Then one nudge.
                    string? why = null;
                    if (JsonIn(resp.Text) is JsonObject json && json.ContainsKey("steps"))
                    {
                        var check = PlanGate.CheckPlan(json, _m);
                        if (check.Ok) return await GuardPlanAsync(check.Plan!, ct);
                        why = check.Error;
                    }
                    if (nudged) throw new ModelCallException(Prompts.NoPlan, null);
                    nudged = true;
                    msgs.Add(new ChatMessage(ChatRole.User, why is null ? Prompts.CallSubmitPlan : $"That plan isn't valid: {why}. {Prompts.CallSubmitPlan}"));
                    continue;
                }

                var results = new List<AIContent>();
                foreach (var fc in calls)
                {
                    var (args, argError) = ArgsOf(fc);
                    if (argError is not null)
                    {
                        results.Add(Reply(fc, Error(argError)));
                        continue;
                    }
                    switch (fc.Name)
                    {
                        case "submit_plan":
                        {
                            var check = PlanGate.CheckPlan(args ?? new JsonObject(), _m);
                            if (check.Ok) return await GuardPlanAsync(check.Plan!, ct);
                            results.Add(Reply(fc, Error(check.Error!)));
                            break;
                        }
                        case "cannot_do":
                        {
                            var v = ArgValidator.Validate(cannotDo, args);
                            if (!v.Ok)
                            {
                                results.Add(Reply(fc, Error(v.Error!)));
                                break;
                            }
                            await CannotDoAsync(v.Clean!, ct);
                            return null;
                        }
                        default:
                            results.Add(Reply(fc, _m.Action(fc.Name) is { Kind: ActionKind.Read } spec
                                ? await PlanReadAsync(spec, args, ct)
                                : Error(Prompts.PlanningCantChange)));
                            break;
                    }
                }
                msgs.Add(new ChatMessage(ChatRole.Tool, results));
            }
        }

        /// <summary>A sensor read while planning: the reading event only (contract), no call or result.</summary>
        async Task<JsonObject> PlanReadAsync(ActionSpec spec, JsonNode? args, CancellationToken ct)
        {
            var split = ArgValidator.SplitStep(args);
            if (split.Error is not null) return Error(split.Error);
            var v = ArgValidator.Validate(spec.Parameters, split.Args);
            if (!v.Ok) return Error(v.Error!);
            Emit(Ev.Reading(spec.Device));
            var o = _sim.Execute(new ActionCall(spec.Name, v.Clean!, null), _room);
            Observe(spec, o);
            await WaitMs(o.DurationMs, ct);
            return (JsonObject)o.ModelResult.DeepClone();
        }

        async Task<ApprovedPlan> GuardPlanAsync(ApprovedPlan plan, CancellationToken ct)
        {
            var texts = await GuardAsync("plan", plan.Steps.Select(s => s.Text).ToList(), _opt.PlanModel, _opt.MaxCompletionTokens,
                i => Prompts.StepFallback(plan.Steps[i], _m, i + 1), _ => ToolCatalog.MaxStepText, ct);
            for (var i = 0; i < plan.Steps.Count; i++) plan.Steps[i] = plan.Steps[i] with { Text = texts[i] };
            return plan;
        }

        async Task CannotDoAsync(JsonObject args, CancellationToken ct)
        {
            var texts = new List<string> { ArgValidator.Str(args["reason"])! };
            texts.AddRange(args["suggestions"]!.AsArray().Select(x => ArgValidator.Str(x)!));
            var clean = await GuardAsync("cannot_do", texts, _opt.PlanModel, _opt.MaxCompletionTokens,
                i => i == 0 || _m.Suggestions.Count == 0 ? Prompts.CannotDoFallback : _m.Suggestions[(i - 1) % _m.Suggestions.Count],
                _ => int.MaxValue, ct);
            Emit(Ev.CannotDo(clean[0], clean.Skip(1).ToList()));
            _turn = new Turn(_text, $"I couldn't plan that: {clean[0]}");
        }

        // ---- 3. run ----

        public async Task RunPhaseAsync(ApprovedPlan plan, CancellationToken ct)
        {
            _tracker ??= new StepTracker(_s.Events, _r.Rid, plan);
            var client = _o._chatFor(_opt.RunModel);
            var msgs = new List<ChatMessage> { new(ChatRole.System, Prompts.RunSystem(_m, _room, Clock(), plan)) };
            // A replay starts from the request and its plan: the history ends with the replayed request's own outcome.
            if (!_r.IsWhatIf) AddHistory(msgs);
            msgs.Add(new ChatMessage(ChatRole.User, _text));
            msgs.Add(new ChatMessage(ChatRole.Assistant, Prompts.PlanMessage(plan)));
            msgs.Add(new ChatMessage(ChatRole.User, Prompts.Approved));
            var options = new ChatOptions
            {
                ModelId = _opt.RunModel,
                Tools = ToolCatalog.RunTools(_m),
                ToolMode = ChatToolMode.Auto,
                MaxOutputTokens = _opt.MaxCompletionTokens,
            };
            while (true)
            {
                var resp = await CallAsync(client, msgs, options, ct);
                msgs.AddRange(resp.Messages);
                var calls = CallsIn(resp);
                if (calls.Count == 0) return; // "DONE", or any text: the loop ends when the model stops calling tools
                var results = new List<AIContent>();
                foreach (var fc in calls)
                {
                    // One response can hold many paced calls: the 3-minute bound holds between them too.
                    if (_budget.Remaining <= TimeSpan.Zero) throw new BudgetExceededException("time");
                    results.Add(Reply(fc, await RunCallAsync(fc, plan, paused: false, ct)));
                }
                msgs.Add(new ChatMessage(ChatRole.Tool, results));
            }
        }

        /// <summary>
        /// Checks one call, then runs it. A refusal or a validation error is a tool error the model can correct: it
        /// emits nothing and changes nothing (the model call that made it still counted). While the clock is paused
        /// (the fault turn) only FaultTurnTools are taken, and a device call only when it is a safe-state call.
        /// </summary>
        async Task<JsonObject> RunCallAsync(FunctionCallContent fc, ApprovedPlan plan, bool paused, CancellationToken ct)
        {
            var (args, argError) = ArgsOf(fc);
            if (argError is not null) return Error(argError);
            var schemas = paused ? _faultSchemas : _runSchemas;
            if (paused && !schemas.ContainsKey(fc.Name)) return Error(Prompts.NotWhilePaused(fc.Name));
            if (_m.Action(fc.Name) is { } spec) return await DeviceCallAsync(spec, args, plan, safeOnly: paused, ct);
            if (!schemas.TryGetValue(fc.Name, out var schema)) return Error($"unknown tool '{fc.Name}'");
            var v = ArgValidator.Validate(schema, args);
            if (!v.Ok) return Error(v.Error!);
            var clean = v.Clean!; // optional nulls are dropped here: read every field from it, never from the raw args
            var g = PlanGate.CheckTool(fc.Name, clean, plan, _m);
            if (!g.Allowed) return Error(g.Error!);
            return fc.Name switch
            {
                "notify_user" => Notify(clean, plan),
                "schedule" => Schedule(clean, plan),
                "add_rule" => AddRule(clean, plan),
                "run_until" => await RunUntilAsync(clean, plan, ct),
                "propose_replan" => await ProposeReplanAsync(clean, plan, ct),
                _ => Error($"unknown tool '{fc.Name}'"),
            };
        }

        /// <param name="safeOnly">The fault turn: the tool schema pins safe values, but only as advice, so it is enforced here.</param>
        async Task<JsonObject> DeviceCallAsync(ActionSpec spec, JsonNode? args, ApprovedPlan plan, bool safeOnly, CancellationToken ct)
        {
            var split = ArgValidator.SplitStep(args);
            if (split.Error is not null) return Error(split.Error);
            var v = ArgValidator.Validate(spec.Parameters, split.Args);
            if (!v.Ok) return Error(v.Error!);
            var call = new ActionCall(spec.Name, v.Clean!, split.Step);
            var g = PlanGate.Check(call, plan, _m);
            if (!g.Allowed) return Error(g.Error!);
            if (safeOnly && !g.Safe) return Error(Prompts.PausedSafeOnly(_m.Devices.SelectMany(d => d.Actions).Where(a => _faultSchemas.ContainsKey(a.Name))));
            // A read or safe-state call needs no approval (spec 3.5). With no step, or a step that doesn't list its
            // device, it runs stepless: step null on its call and result, and no step event.
            if (spec.Kind == ActionKind.Read || g.Safe)
                call = call with { Step = Known(call.Step, plan, spec.Device) };
            return await ExecuteCallAsync(spec, call, ct);
        }

        /// <summary>Emits step, reading, call, the what-if fault, each state and the result, paced like the device.</summary>
        async Task<JsonObject> ExecuteCallAsync(ActionSpec spec, ActionCall call, CancellationToken ct)
        {
            var callId = $"c{++_callSeq}";
            _tracker!.OnCall(call.Step);
            if (spec.Kind == ActionKind.Read) Emit(Ev.Reading(spec.Device));
            Emit(Ev.Call(call.Step, callId, spec.Device, spec.Name, call.Args, _b.Label(spec, call.Args)));
            // A what-if device turns red at its first failing call (spec 3.4), not before.
            if (_room.Failed.Contains(spec.Device) && _room.Faulted.Add(spec.Device)) EmitFault(spec.Device);

            double? baseline = ConfirmedBy.TryGetValue(spec.Name, out var by) ? ArgValidator.Num(_room.Get(by.Path)) : null;
            var o = _sim.Execute(call, _room);
            if (o.Outcome == Outcome.Invalid)
                throw new InvalidOperationException($"{spec.Name} passed validation but the simulator refused it: {o.Text}");
            Observe(spec, o);

            // The simulator left the room at the action's final values; each frame plays its own value, in time. An
            // edit made in one of the waits below makes the room the visitor's for its fields, and bumps their stamps,
            // so the AI's later frames for them are dropped and the visitor's value stays the last one (spec 3.4).
            var stamps = EditApplier.TakeStamps(_s, o.Changes.Select(c => c.Path));
            // DelayMs counts from the start of the action, so wait only for the gap since the last change.
            var at = 0;
            foreach (var c in o.Changes.OrderBy(c => c.DelayMs))
            {
                await WaitMs(c.DelayMs - at, ct);
                at = Math.Max(at, c.DelayMs);
                EditApplier.EmitIfNewest(_s, _r.Rid, Ev.State(c), c.Path, stamps);
            }
            await WaitMs(o.DurationMs - at, ct);
            Emit(Ev.Result(callId, call.Step, spec.Device, o.Ok, Ev.Wire(o.Outcome), o.Text));
            _tracker.OnResult(call.Step, o.Ok, spec.Device);
            RecordCall(spec, call, o, baseline);
            return (JsonObject)o.ModelResult.DeepClone();
        }

        /// <summary>A reading the AI got: the only sensor value later prompts may show.</summary>
        void Observe(ActionSpec spec, ActionOutcome o)
        {
            if (o.Outcome == Outcome.Reading && o.ModelResult["reading"] is JsonNode reading)
                _room.Observed[spec.Device] = new JsonObject { ["reading"] = reading.DeepClone(), ["at"] = Clock().Label };
        }

        /// <summary>The simulator's text for a device set to fail (spec 5.4).</summary>
        const string FaultText = "not responding";

        /// <summary>fault, then status = "fault" set and emitted: at a first failing call, a fault turn, or summary time.</summary>
        void EmitFault(string device)
        {
            Emit(Ev.Fault(device, FaultText)); // the short reason; the demo adds the device name (contract)
            var change = new StateChange($"{device}.status", JsonValue.Create("fault"), 0);
            _room.Set(change.Path, change.Value);
            EmitState(change);
        }

        JsonObject Notify(JsonObject args, ApprovedPlan plan)
        {
            var text = Clean("notify", ArgValidator.Str(args["text"])!, Prompts.NotifyFallback);
            var step = Known(ArgValidator.Str(args["step"]), plan);
            _tracker!.OnCall(step);
            var row = new TimelineRow(Clock().Label, text, true, "notify", null, step);
            _timeline.Add(row);
            Emit(Ev.Timeline(row));
            _tracker.OnResult(step, true);
            _facts.Add(new JsonObject { ["tool"] = "notify_user", ["text"] = text });
            return new JsonObject { ["ok"] = true };
        }

        /// <summary>"HH:MM" daily (optionally on some days), or "yyyy-MM-ddTHH:mm" once; both parsed exactly.</summary>
        JsonObject Schedule(JsonObject args, ApprovedPlan plan)
        {
            if (RoomFull()) return Error(Prompts.AutomationsFull(Automations.MaxPerRoom));
            var at = ArgValidator.Str(args["at"])!;
            string? daily = null;
            double? once = null;
            if (TimeOnly.TryParseExact(at, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) daily = at;
            else if (DateTime.TryParseExact(at, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var when))
            {
                once = (when - _m.StartDay.Date).TotalHours;
                if (once <= _room.Hour + 1e-9) return Error($"'at' {at} has passed; the clock reads {Clock().Iso[..16]}");
            }
            else return Error($"'at' {at} isn't a real time; use \"HH:MM\" or \"yyyy-MM-ddTHH:mm\"");

            HashSet<DayOfWeek>? days = null;
            if (args["days"] is JsonArray list)
            {
                if (daily is null) return Error("'days' goes with a daily \"HH:MM\" time only");
                days = list.Select(d => Day(ArgValidator.Str(d)!)).ToHashSet();
            }
            var (spec, inner) = Inner(args["action"], args["args"]);
            var step = ArgValidator.Str(args["step"])!;
            var id = NextId("sched", _room.Schedules.Select(x => x.Id));
            _room.Schedules.Add(new Schedule(id, step, new ActionCall(spec.Name, inner, step), daily, days, once) { Order = _room.NextOrder() });
            _added.Add(id);
            EmitAutomations(); // its card appears as the AI makes it
            Track(Known(step, plan, spec.Device));
            _facts.Add(new JsonObject { ["tool"] = "schedule", ["id"] = id, ["at"] = at, ["action"] = _b.Label(spec, inner) });
            return new JsonObject { ["ok"] = true, ["id"] = id };
        }

        /// <summary>The room holds Automations.MaxPerRoom rules and schedules: a new one is refused with a tool error.</summary>
        bool RoomFull() => _room.Rules.Count + _room.Schedules.Count >= Automations.MaxPerRoom;

        JsonObject AddRule(JsonObject args, ApprovedPlan plan)
        {
            if (RoomFull()) return Error(Prompts.AutomationsFull(Automations.MaxPerRoom));
            var when = args["when"]!.AsObject();
            var then = args["then"]!.AsObject();
            var path = $"{ArgValidator.Str(when["device"])}.{ArgValidator.Str(when["field"])}";
            var op = ArgValidator.Str(when["op"]) switch { "below" => RuleOp.Below, "above" => RuleOp.Above, _ => RuleOp.Becomes };
            var step = ArgValidator.Str(args["step"])!;
            var device = ArgValidator.Str(when["device"])!; // a notify rule's step should list the device it watches
            ActionCall? action = null;
            string? notify = null;
            string label;
            if (ArgValidator.Str(then["action"]) is not null)
            {
                var (spec, inner) = Inner(then["action"], then["args"]);
                action = new ActionCall(spec.Name, inner, step);
                label = _b.Label(spec, inner);
                device = spec.Device;
            }
            else
            {
                notify = Clean("rule notify", ArgValidator.Str(then["notify"])!, Prompts.NotifyFallback);
                label = $"notify: {notify}";
            }
            var maxPerDay = (int)(ArgValidator.Num(args["max_per_day"]) ?? 2);
            var id = NextId("rule", _room.Rules.Select(x => x.Id));
            // Seeded with what the field reads now, so a "becomes" that already holds waits for a change.
            _room.Rules.Add(new Rule(id, step, path, op, when["value"]!.DeepClone(), action, notify, maxPerDay)
            {
                LastSeen = _room.Get(path)?.DeepClone(),
                Order = _room.NextOrder(),
            });
            _added.Add(id);
            EmitAutomations(); // its card appears as the AI makes it
            Track(Known(step, plan, device));
            _facts.Add(new JsonObject
            {
                ["tool"] = "add_rule", ["id"] = id,
                ["when"] = $"{path} {ArgValidator.Str(when["op"])} {when["value"]!.ToJsonString()}", ["then"] = label,
            });
            return new JsonObject { ["ok"] = true, ["id"] = id };
        }

        // ---- fast-forward (spec 5.3) ----

        /// <summary>
        /// Moves the sim clock without the model: the sink streams clock, state and timeline, and a device's first
        /// fault pauses the clock for one model turn. The model gets a compact report. A fault turn's re-plan that
        /// got no answer stops the fast-forward (Paused) and ends the request expired.
        /// The clock moves at most the approved plan's horizon in all, and its pacing counts against the work time
        /// (approval waits in a fault turn excluded): running out ends the request limit {time}.
        /// </summary>
        async Task<JsonObject> RunUntilAsync(JsonObject args, ApprovedPlan plan, CancellationToken ct)
        {
            if (plan.HorizonHours is not double horizon) return Error(Prompts.NoHorizon);
            var left = horizon - _fastForwarded;
            if (left < 1e-6) return Error(Prompts.HorizonUsed(horizon));
            var asked = ArgValidator.Num(args["hours"])!.Value;
            var hours = Math.Min(asked, left);

            using var work = new CancellationTokenSource(_budget.Remaining, _o._tp);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, work.Token);
            var sink = new Sink(this, plan, work, ct);
            var from = _room.Hour;
            // Edits apply in the pacer's waits, at the current tick; sim time is compressed, so their frames go out at once.
            var pacer = new EditServicingPacer(_o._pacer, _s.Edits.WhenPending, () => ServiceEdits(immediate: true));
            FastForwardReport report;
            try
            {
                report = await new FastForward(_sim, _b, pacer, _opt.Pacing).RunAsync(_room, hours, sink, linked.Token);
            }
            catch (OperationCanceledException) when (work.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new BudgetExceededException("time");
            }
            finally
            {
                _fastForwarded += _room.Hour - from;
                EmitAutomations(); // a step the clock stopped in may not have streamed its fires
            }
            if (report.Paused) throw new ReplanExpiredException();
            // The box polls what a rule that is on at the end watches, so those sensors count as read; no other sensor does.
            var watched = _room.Rules.Where(r => r.Enabled).Select(r => r.Path[..r.Path.IndexOf('.')]).ToHashSet(StringComparer.Ordinal);

            var (rows, more) = Compact(report.Rows);
            var result = new JsonObject { ["ok"] = true, ["hours"] = hours, ["clock"] = Clock().Label, ["rows"] = rows };
            if (hours < asked) result["note"] = Prompts.HorizonClamped(horizon, hours);
            if (more > 0) result["moreRows"] = more;
            if (sink.Pauses.Count > 0) result["pauses"] = new JsonArray(sink.Pauses.Select(x => (JsonNode?)x).ToArray());
            result["endReadings"] = PolledReadings(report.EndReadings, watched);
            return result;
        }

        /// <summary>
        /// Fast-forward's end readings with the read-only sensor rule: a sensor a rule watched reports what the box
        /// polled, which becomes its last observed reading; any other sensor is "not read". A failed device keeps its
        /// error only once the run has hit it: before that it is "not read", since the AI isn't told in advance (spec 3.4).
        /// </summary>
        JsonObject PolledReadings(JsonObject end, IReadOnlySet<string> watched)
        {
            foreach (var d in _m.Devices)
            {
                if (_room.Failed.Contains(d.Id))
                {
                    if (!_room.Faulted.Contains(d.Id)) end[d.Id] = "not read";
                    continue;
                }
                if (!Prompts.IsSensor(d)) continue;
                var read = d.Actions.First(a => a.Kind == ActionKind.Read);
                var o = watched.Contains(d.Id) ? _sim.Execute(new ActionCall(read.Name, new JsonObject(), null), _room) : null;
                if (o is not { Outcome: Outcome.Reading } || o.ModelResult["reading"] is not JsonNode reading)
                {
                    end[d.Id] = "not read";
                    continue;
                }
                end[d.Id] = reading.DeepClone();
                Observe(read, o);
                _facts.Add(new JsonObject
                {
                    ["tool"] = "run_until", ["device"] = d.Id, ["outcome"] = "reading", ["reading"] = reading.DeepClone(),
                    ["text"] = $"read by a rule at {Clock().Label}",
                });
            }
            return end;
        }

        void AddRow(TimelineRow row)
        {
            _timeline.Add(row);
            if (row.Kind == "fault") _fastForwardFault = true;
            Emit(Ev.Timeline(row));
            // A device's first fault row is the fault turn's to report; a later one fails its own step now.
            if (row.Kind == "fault" && row.Device is string d && _room.Faulted.Contains(d)) FailStep(row);
        }

        /// <summary>The checklist matches done failed: the step whose schedule or rule hit the failed device fails.</summary>
        void FailStep(TimelineRow row)
        {
            if (_plan is { } plan) _tracker?.Fail(Known(row.Step, plan, row.Device), row.Device);
        }

        /// <summary>
        /// The paused clock's one model turn (spec 5.3), after the fault row: fault, then status "fault", then one model
        /// call with FaultTurnTools. Its calls are gated as in the run, safe-state only. True resumes the clock; false
        /// means its re-plan expired.
        /// </summary>
        async Task<bool> FaultTurnAsync(string device, TimelineRow row, ApprovedPlan plan, List<JsonObject> pauses, CancellationToken ct)
        {
            EmitFault(device);
            FailStep(row);
            var msgs = new List<ChatMessage>
            {
                new(ChatRole.System, Prompts.FaultTurnSystem(_m, _room, Clock(), plan)),
                new(ChatRole.User, _text),
                new(ChatRole.Assistant, Prompts.PlanMessage(plan)),
                new(ChatRole.User, Prompts.FaultTurn(row, Compact(_timeline).Rows)),
            };
            var options = new ChatOptions
            {
                ModelId = _opt.RunModel,
                Tools = _faultTools,
                ToolMode = ChatToolMode.Auto,
                MaxOutputTokens = _opt.MaxCompletionTokens,
            };
            var resp = await CallAsync(_o._chatFor(_opt.RunModel), msgs, options, ct);
            var did = new JsonArray();
            pauses.Add(new JsonObject { ["at"] = row.At, ["fault"] = row.Text, ["results"] = did });
            foreach (var fc in CallsIn(resp))
            {
                if (_budget.Remaining <= TimeSpan.Zero) throw new BudgetExceededException("time");
                try
                {
                    did.Add($"{fc.Name}: {Prompts.ToJson(await RunCallAsync(fc, plan, paused: true, ct))}");
                }
                catch (ReplanExpiredException)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The runner's side of fast-forward: events out, and the fault turn. The work-time token stops counting during
        /// the turn: its model call and calls have their own bounds, and its approval wait isn't work time.
        /// </summary>
        sealed class Sink(Run run, ApprovedPlan plan, CancellationTokenSource work, CancellationToken ct) : IFastForwardSink
        {
            /// <summary>Each fault turn: when, the fault, and what its calls returned.</summary>
            public List<JsonObject> Pauses { get; } = new();

            public Task ClockAsync(Clock c)
            {
                run.Emit(Ev.ClockEv(c));
                return Task.CompletedTask;
            }

            public Task StateAsync(StateChange s)
            {
                run.EmitState(s);
                return Task.CompletedTask;
            }

            public Task RowAsync(TimelineRow r)
            {
                run.AddRow(r);
                return Task.CompletedTask;
            }

            /// <summary>Fires and a new day change the automations list: it streams with the request's rid.</summary>
            public Task StepDoneAsync()
            {
                run.EmitAutomations();
                return Task.CompletedTask;
            }

            public async Task<bool> FirstFaultAsync(string device, TimelineRow r)
            {
                work.CancelAfter(Timeout.InfiniteTimeSpan);
                try
                {
                    return await run.FaultTurnAsync(device, r, plan, Pauses, ct);
                }
                finally
                {
                    work.CancelAfter(run._budget.Remaining);
                }
            }
        }

        /// <summary>Timeline rows for the model: repeats (same kind, text and result) collapsed as "×N", at most 40.</summary>
        static (JsonArray Rows, int More) Compact(IEnumerable<TimelineRow> rows, int max = 40)
        {
            var groups = rows.GroupBy(r => (r.Kind, r.Text, r.Ok)).ToList(); // in order of first appearance
            var list = new JsonArray();
            foreach (var g in groups.Take(max))
            {
                var first = g.First();
                var text = first.Kind == "notify" ? $"sent you: {first.Text}" : first.Ok || first.Kind == "fault" ? first.Text : $"{first.Text} (failed)";
                var n = g.Count();
                list.Add(n == 1 ? $"{first.At} {text}" : $"{first.At} {text} ×{n}, last {g.Last().At}");
            }
            return (list, Math.Max(0, groups.Count - max));
        }

        // ---- re-plans (spec 3.5) ----

        /// <summary>
        /// Checks the steps and gives them their final ids before anything is emitted, guards their text, then asks
        /// the visitor. Approved: merged into the plan, and the schedules and rules that lost their device are dropped.
        /// Declined: no event, the model carries on. No answer: the request ends expired.
        /// </summary>
        async Task<JsonObject> ProposeReplanAsync(JsonObject args, ApprovedPlan plan, CancellationToken ct)
        {
            var check = PlanGate.CheckReplan(args, plan, _m);
            if (!check.Ok) return Error(check.Error!);
            var steps = check.Steps!.ToList();
            var texts = steps.Select(s => s.Text).Append(ArgValidator.Str(args["reason"])!).ToList();
            var clean = await GuardAsync("replan", texts, _opt.RunModel, _opt.MaxCompletionTokens,
                i => i < steps.Count ? Prompts.StepFallback(steps[i], _m, StepNumber(steps[i].Id, i)) : Prompts.ReplanReasonFallback,
                i => i < steps.Count ? ToolCatalog.MaxStepText : int.MaxValue, ct);
            for (var i = 0; i < steps.Count; i++) steps[i] = steps[i] with { Text = clean[i] };

            Emit(Ev.Replan(steps, clean[^1]));
            var d = await AskAsync("replan", ct);
            if (d is null) throw new ReplanExpiredException();
            var ids = new JsonArray(steps.Select(s => (JsonNode?)s.Id).ToArray());
            if (d == Decision.Decline)
            {
                _facts.Add(new JsonObject { ["tool"] = "propose_replan", ["steps"] = ids, ["answer"] = "declined" });
                return new JsonObject { ["ok"] = true, ["answer"] = Prompts.ReplanDeclined };
            }

            plan.Merge(steps);
            var removed = DropUnplanned(plan);
            _facts.Add(new JsonObject
            {
                ["tool"] = "propose_replan", ["steps"] = ids, ["answer"] = "approved",
                ["removed"] = new JsonArray(removed.Select(t => (JsonNode?)t).ToArray()),
            });
            var result = new JsonObject
            {
                ["ok"] = true, ["answer"] = Prompts.ReplanApproved,
                ["steps"] = new JsonArray(steps.Select(s => (JsonNode?)new JsonObject
                {
                    ["id"] = s.Id, ["text"] = s.Text, ["devices"] = new JsonArray(s.Devices.Select(x => (JsonNode?)x).ToArray()),
                }).ToArray()),
            };
            if (removed.Count > 0) result["removed"] = new JsonArray(removed.Select(t => (JsonNode?)t).ToArray());
            return result;
        }

        /// <summary>
        /// After a merge: every schedule and rule this run added whose action the new plan no longer allows (a replaced
        /// step lost its device) is removed, each with a failed timeline row. Safe-state actions and notify rules stay,
        /// and so does whatever earlier requests set up: their step ids belong to their own plans.
        /// </summary>
        List<string> DropUnplanned(ApprovedPlan plan)
        {
            var removed = new List<string>();
            void Drop(ActionCall call, string step, string what)
            {
                var device = _m.Action(call.Action)!.Device;
                var row = new TimelineRow(Clock().Label, Prompts.RemovedByReplan(_m.Device(device)?.Name ?? device, what), false, "action", device, step);
                Emit(Ev.Timeline(row));
                removed.Add(row.Text);
            }
            foreach (var s in _room.Schedules.Where(x => _added.Contains(x.Id)).ToList())
                if (!PlanGate.Check(s.Call with { Step = s.Step }, plan, _m).Allowed)
                {
                    _room.Schedules.Remove(s);
                    Drop(s.Call, s.Step, "schedule");
                }
            foreach (var r in _room.Rules.Where(x => _added.Contains(x.Id)).ToList())
                if (r.Then is { } then && !PlanGate.Check(then with { Step = r.Step }, plan, _m).Allowed)
                {
                    _room.Rules.Remove(r);
                    Drop(then, r.Step, "rule");
                }
            if (removed.Count > 0) EmitAutomations(); // their cards go
            return removed;
        }

        /// <summary>"s4" → 4, for a step's fallback name; the position otherwise.</summary>
        static int StepNumber(string id, int i) => id.Length > 1 && int.TryParse(id.AsSpan(1), out var n) ? n : i + 1;

        /// <summary>schedule and add_rule's inner {action, args}, already gated: the spec and the cleaned args.</summary>
        (ActionSpec Spec, JsonObject Args) Inner(JsonNode? action, JsonNode? args)
        {
            var spec = _m.Action(ArgValidator.Str(action)!)!;
            return (spec, ArgValidator.Validate(spec.Parameters, args).Clean!);
        }

        void Track(string? step)
        {
            _tracker!.OnCall(step);
            _tracker.OnResult(step, true);
        }

        void RecordCall(ActionSpec spec, ActionCall call, ActionOutcome o, double? baseline)
        {
            var fact = new JsonObject
            {
                ["device"] = spec.Device, ["action"] = spec.Name, ["step"] = call.Step,
                ["outcome"] = Ev.Wire(o.Outcome), ["text"] = o.Text,
            };
            if (o.ModelResult["reading"] is JsonNode reading) fact["reading"] = reading.DeepClone();
            if (o.Outcome == Outcome.Commanded)
            {
                fact["confirmed"] = false;
                if (baseline is double b && ConfirmedBy.TryGetValue(spec.Name, out var by)) _unconfirmed.Add(new Pending(fact, by.Read, by.Path, b));
            }
            if (o.Outcome == Outcome.Reading)
                foreach (var p in _unconfirmed.Where(p => p.Read == spec.Name).ToList())
                    if (ArgValidator.Num(_room.Get(p.Path)) is double now && now > p.Baseline + 1e-9)
                    {
                        p.Fact["confirmed"] = true;
                        _unconfirmed.Remove(p);
                    }
            _facts.Add(fact);
        }

        // ---- 4. summary ----

        public async Task<string> SummaryAsync(CancellationToken ct)
        {
            _tracker?.Finish();
            // A what-if device the run never called or hit turns red now, and the summary says so (spec 3.4 step 5).
            var untouched = FlagUntouched();
            var facts = Facts();
            if (untouched.Count > 0) facts["untouchedFailedDevice"] = string.Join(" and ", untouched.Select(d => _m.Device(d)?.Ref ?? d));
            var client = _o._chatFor(_opt.RunModel);
            var msgs = new List<ChatMessage>
            {
                new(ChatRole.System, Prompts.SummarySystem(_m)),
                new(ChatRole.User, Prompts.SummaryInstruction(facts)),
            };
            var options = new ChatOptions { ModelId = _opt.RunModel, MaxOutputTokens = SummaryMaxTokens };
            var say = new SayStream(delta => Emit(Ev.Say(delta)));
            // Not a servicing wait: an edit made while the summary streams applies when the request finishes.
            var text = await StreamAsync(client, msgs, options, say.Add, ct);
            say.End();
            var guarded = (await GuardAsync("summary", new[] { text.Trim() }, _opt.RunModel, SummaryMaxTokens,
                _ => Prompts.SummaryFallback, _ => int.MaxValue, ct))[0];
            var final = CopyGuard.TwoSentences(guarded);
            if (string.IsNullOrWhiteSpace(final)) final = Prompts.SummaryFallback;
            _summary = final;
            Emit(Ev.Summary(final));
            _turn = TurnWith(final);
            // A failed step fails the request, and so does a device that didn't respond while time passed.
            return _tracker?.AnyFailed == true || _fastForwardFault ? "failed" : "ok";
        }

        /// <summary>Every failed device not yet red: fault and status now. Returns them, in order.</summary>
        List<string> FlagUntouched()
        {
            var untouched = _room.Failed.Where(d => !_room.Faulted.Contains(d)).Order(StringComparer.Ordinal).ToList();
            foreach (var d in untouched)
            {
                _room.Faulted.Add(d);
                EmitFault(d);
            }
            return untouched;
        }

        /// <summary>The server's account of the run, for the summary call.</summary>
        JsonObject Facts()
        {
            var failed = new SortedSet<string>(_room.Failed, StringComparer.Ordinal);
            foreach (var f in _facts)
                if (ArgValidator.Str(f["outcome"]) == "failed" && ArgValidator.Str(f["device"]) is string d) failed.Add(d);
            return new JsonObject
            {
                ["request"] = _text,
                ["steps"] = new JsonArray(_plan!.Steps.Select(s => (JsonNode?)new JsonObject
                {
                    ["id"] = s.Id, ["text"] = s.Text, ["status"] = _tracker?.StatusOf(s.Id),
                }).ToArray()),
                ["calls"] = new JsonArray(_facts.Select(f => (JsonNode?)f.DeepClone()).ToArray()),
                ["timeline"] = new JsonObject
                {
                    ["list"] = Compact(_timeline).Rows,
                    ["rows"] = _timeline.Count,
                    ["notify"] = _timeline.Count(t => t.Kind == "notify"),
                    ["faults"] = _timeline.Count(t => t.Kind == "fault"),
                    ["failedActions"] = _timeline.Count(t => t.Kind == "action" && !t.Ok),
                },
                ["failedDevices"] = new JsonArray(failed.Select(d => (JsonNode?)d).ToArray()),
                ["endReadings"] = EndReadings(),
            };
        }

        JsonObject EndReadings()
        {
            var end = _room.ToWire();
            foreach (var (dev, node) in end.ToList())
            {
                if (_room.Failed.Contains(dev))
                {
                    end[dev] = new JsonObject { ["error"] = "not responding" };
                    continue;
                }
                if (_m.Device(dev) is { } d && Prompts.IsSensor(d))
                {
                    // A sensor reports only what this run read from it, never the room's own value.
                    end[dev] = LastReading(dev)?.DeepClone() ?? JsonValue.Create("not read");
                    continue;
                }
                if (node is JsonObject o)
                    foreach (var (key, v) in o.ToList())
                        if (ArgValidator.Num(v) is double x) o[key] = Math.Round(x, 2);
            }
            return end;
        }

        JsonNode? LastReading(string device) =>
            _facts.LastOrDefault(f => ArgValidator.Str(f["device"]) == device && ArgValidator.Str(f["outcome"]) == "reading")?["reading"];

        // ---- the copy guard on model text ----

        /// <summary>
        /// On a hit, one rephrase call for all the texts together; whatever still hits is stripped. A text that ends up
        /// empty, or still hits after the strip, becomes its fixed line.
        /// </summary>
        async Task<List<string>> GuardAsync(string kind, IReadOnlyList<string> texts, string model, int maxTokens,
            Func<int, string> fallback, Func<int, int> maxLen, CancellationToken ct)
        {
            var hits = texts.SelectMany(CopyGuard.Find).ToList();
            if (hits.Count == 0) return texts.Select((t, i) => string.IsNullOrWhiteSpace(t) ? fallback(i) : t).ToList();
            LogHit(kind, hits);
            // The rephrase is best-effort: a call the budget, the kill switch or the service refuses means strip
            // instead, and the request carries on. A cancelled session (ct) is not caught.
            ChatResponse? resp = null;
            try
            {
                resp = await CallAsync(_o._chatFor(model),
                    new List<ChatMessage> { new(ChatRole.User, Prompts.Rephrase(texts, Distinct(hits))) },
                    new ChatOptions { ModelId = model, MaxOutputTokens = maxTokens }, ct);
            }
            catch (Exception e) when (e is BudgetExceededException or TokenBudgetSpentException or ModelCallException)
            {
                _o._log.LogWarning(RephraseSkipped, "Rephrase for {TextKind} skipped ({Reason}); stripping instead",
                    kind, e is BudgetExceededException b ? $"{b.Kind} budget" : e is TokenBudgetSpentException ? "token budget" : "model call failed");
            }
            var again = resp is null ? null : Rephrased(resp.Text, texts.Count);
            var result = new List<string>();
            for (var i = 0; i < texts.Count; i++)
            {
                var candidate = again?[i].Trim() is { Length: > 0 } t && t.Length <= maxLen(i) ? t : texts[i];
                // Without a rephrase the candidate is the text whose hit was just logged.
                result.Add(resp is null ? Clean(null, candidate, fallback(i)) : Clean($"{kind} after rephrase", candidate, fallback(i)));
            }
            return result;
        }

        /// <summary>
        /// No rephrase: strip on a hit, and fall back to the fixed line if that leaves nothing clean. A null kind means
        /// the hit is already logged.
        /// </summary>
        string Clean(string? kind, string text, string fallback)
        {
            var hits = CopyGuard.Find(text);
            if (hits.Count == 0) return string.IsNullOrWhiteSpace(text) ? fallback : text;
            if (kind is not null) LogHit(kind, hits);
            var stripped = CopyGuard.Strip(text);
            return string.IsNullOrWhiteSpace(stripped) || CopyGuard.Find(stripped).Count > 0 ? fallback : stripped;
        }

        /// <summary>The kind of text and the matched terms only, never the visitor's or the model's text.</summary>
        void LogHit(string kind, IEnumerable<string> hits) =>
            _o._log.LogWarning(CopyGuardHit, "Copy guard hit in {TextKind}: {Terms}", kind, string.Join(", ", Distinct(hits)));

        static List<string> Distinct(IEnumerable<string> hits) => hits.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>A JSON array of n strings; for a single text, the plain reply is accepted too.</summary>
        static List<string>? Rephrased(string text, int n)
        {
            int start = text.IndexOf('['), end = text.LastIndexOf(']');
            if (start >= 0 && end > start)
                try
                {
                    if (JsonNode.Parse(text[start..(end + 1)]) is JsonArray a && a.Count == n && a.All(x => ArgValidator.Str(x) is not null))
                        return a.Select(x => ArgValidator.Str(x)!).ToList();
                }
                catch (JsonException) { }
            return n == 1 && !string.IsNullOrWhiteSpace(text) ? new List<string> { text.Trim() } : null;
        }

        // ---- model calls ----

        void BeforeCall()
        {
            if (_o._meter.Spent(UsageDay.For(Now))) throw new TokenBudgetSpentException();
            _budget.BeforeCall();
        }

        /// <summary>
        /// Runs under ct and the work budget's remaining time, so a stalled call is cancelled when it runs out (limit {time}). A call
        /// that fails or stalls after it may have reached the service is still metered (its prompt estimated), so the
        /// kill switch and the run log see what it may have cost.
        /// </summary>
        async Task<ChatResponse> CallAsync(IChatClient client, List<ChatMessage> msgs, ChatOptions options, CancellationToken ct)
        {
            BeforeCall();
            using var budget = new CancellationTokenSource(_budget.Remaining, _o._tp);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, budget.Token);
            ChatResponse resp;
            try
            {
                resp = await WhileServicing(client.GetResponseAsync(msgs, options, linked.Token));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Account(null, msgs, 0);
                throw;
            }
            catch (OperationCanceledException) when (budget.IsCancellationRequested)
            {
                // The work budget ran out during the call: the request's time bound, not a model failure.
                Account(null, msgs, 0);
                throw new BudgetExceededException("time");
            }
            catch (Exception e)
            {
                if (!NeverSent(e)) Account(null, msgs, 0);
                throw new ModelCallException(Prompts.ModelDown, e);
            }
            Account(resp.Usage, msgs, Chars(resp.Messages));
            return resp;
        }

        /// <summary>The streamed summary, under the same bounds. HttpClient.Timeout alone may not cover a streamed body.</summary>
        async Task<string> StreamAsync(IChatClient client, List<ChatMessage> msgs, ChatOptions options, Action<string> onText, CancellationToken ct)
        {
            BeforeCall();
            using var budget = new CancellationTokenSource(_budget.Remaining, _o._tp);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, budget.Token);
            var text = new StringBuilder();
            UsageDetails? usage = null;
            try
            {
                await foreach (var u in client.GetStreamingResponseAsync(msgs, options, linked.Token).WithCancellation(linked.Token))
                {
                    foreach (var c in u.Contents)
                        if (c is UsageContent uc) (usage ??= new UsageDetails()).Add(uc.Details);
                    var t = u.Text;
                    if (string.IsNullOrEmpty(t)) continue;
                    text.Append(t);
                    onText(t);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Account(usage, msgs, text.Length);
                throw;
            }
            catch (OperationCanceledException) when (budget.IsCancellationRequested)
            {
                Account(usage, msgs, text.Length);
                throw new BudgetExceededException("time");
            }
            catch (Exception e)
            {
                // The prompt, and the words that came, estimated where the service reported nothing.
                if (text.Length > 0 || usage is not null || !NeverSent(e)) Account(usage, msgs, text.Length);
                throw new ModelCallException(Prompts.ModelDown, e);
            }
            Account(usage, msgs, text.Length);
            return text.ToString();
        }

        /// <summary>
        /// True only when no connection was made (DNS, TCP connect, TLS handshake: the cases Senparc.Xncf.NeuCharBoxLive.NeuChar's retry
        /// handler retries), so the request never reached the service and cost nothing. Anything else may have.
        /// </summary>
        static bool NeverSent(Exception e)
        {
            for (Exception? x = e; x is not null; x = x.InnerException)
                if (x is HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError or HttpRequestError.SecureConnectionError
                        or HttpRequestError.NameResolutionError })
                    return true;
            return false;
        }

        /// <summary>Adds the call's tokens (estimated at 4 characters a token where none were reported) to the request and the day.</summary>
        void Account(UsageDetails? u, IEnumerable<ChatMessage> sent, int completionChars)
        {
            var before = _budget.Usage;
            _budget.AddUsage(u, Estimate(Chars(sent)), Estimate(completionChars));
            var after = _budget.Usage;
            var spent = after.PromptTokens - before.PromptTokens + after.CompletionTokens - before.CompletionTokens;
            if (spent > 0) _o._meter.Add(UsageDay.For(Now), spent);
        }

        static int Estimate(int chars) => Math.Max(1, (chars + 3) / 4);

        static int Chars(IEnumerable<ChatMessage> messages) => messages.Sum(m => m.Contents.Sum(c => c switch
        {
            TextContent t => t.Text?.Length ?? 0,
            FunctionCallContent f => f.Name.Length + 16 * (f.Arguments?.Count ?? 0),
            FunctionResultContent r => r.Result?.ToString()?.Length ?? 0,
            _ => 0,
        }));

        // ---- small helpers ----

        void Emit((string kind, JsonObject data) e) => _s.Events.Append(e.kind, _r.Rid, e.data);

        /// <summary>A state event as the newest for its path: an older edit's delayed frame for it can't land after this.</summary>
        void EmitState(StateChange c) => EditApplier.EmitNewest(_s, _r.Rid, Ev.State(c), c.Path);

        /// <summary>The room's automations list, with this request's rid, when it changed since the last one streamed.</summary>
        void EmitAutomations() => EditApplier.EmitAutomations(_s, _room, _r.Rid, edit: false);

        void AddHistory(List<ChatMessage> msgs)
        {
            foreach (var t in _s.History.ToList())
            {
                msgs.Add(new ChatMessage(ChatRole.User, t.User));
                msgs.Add(new ChatMessage(ChatRole.Assistant, t.Assistant));
            }
        }

        async Task WaitMs(int ms, CancellationToken ct)
        {
            if (ms > 0) await WhileServicing(_o._pacer.WaitAsync(TimeSpan.FromMilliseconds(ms), ct));
        }

        // ---- the visitor's edits (spec 3.4) ----

        /// <summary>
        /// Applies the visitor's waiting edits to this request's room. Called only at the runner's waits (and at its
        /// end), never mid-way through applying an action, so the room is never half-changed. Their events carry this
        /// request's rid; a rule they fire joins this request's timeline. No model call, no tokens.
        /// </summary>
        void ServiceEdits(bool immediate = false) =>
            EditApplier.ApplyAll(_s, _editor, _room, _r.Rid, _o._pacer, row => _timeline.Add(row), immediate);

        /// <summary>Awaits a model call or a wait, applying the visitor's edits as they come (and once after).</summary>
        Task<T> WhileServicing<T>(Task<T> work) => EditServicingPacer.Serve(work, _s.Edits.WhenPending, () => ServiceEdits());

        /// <inheritdoc cref="WhileServicing{T}(Task{T})"/>
        Task WhileServicing(Task work) => EditServicingPacer.Serve(work, _s.Edits.WhenPending, () => ServiceEdits());

        /// <summary>The step when it is in the plan (and lists the device, when one is given); otherwise null.</summary>
        static string? Known(string? step, ApprovedPlan plan, string? device = null) =>
            step is not null && plan.Step(step) is { } s && (device is null || s.Devices.Contains(device)) ? step : null;

        static List<FunctionCallContent> CallsIn(ChatResponse r) =>
            r.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Where(f => !f.InformationalOnly).ToList();

        /// <summary>The call's args as JSON, the way ArgValidator takes them. Args a client couldn't parse are a tool error.</summary>
        static (JsonNode? Args, string? Error) ArgsOf(FunctionCallContent fc)
        {
            const string bad = "args must be a JSON object";
            if (fc.Exception is not null) return (null, bad);
            if (fc.Arguments is null) return (null, null);
            try { return (JsonSerializer.SerializeToNode(fc.Arguments, AIJsonUtilities.DefaultOptions), null); }
            catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException or ArgumentException)
            {
                return (null, bad);
            }
        }

        static JsonObject? JsonIn(string text)
        {
            int start = text.IndexOf('{'), end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            try { return JsonNode.Parse(text[start..(end + 1)]) as JsonObject; }
            catch (JsonException) { return null; }
        }

        static JsonObject Error(string message) => new() { ["ok"] = false, ["error"] = message };

        static FunctionResultContent Reply(FunctionCallContent fc, JsonObject result) => new(fc.CallId, Prompts.ToJson(result));

        static string NextId(string prefix, IEnumerable<string> taken)
        {
            var used = taken.ToHashSet(StringComparer.Ordinal);
            var n = used.Count + 1;
            while (used.Contains($"{prefix}{n}")) n++;
            return $"{prefix}{n}";
        }

        static DayOfWeek Day(string d) => d switch
        {
            "Mon" => DayOfWeek.Monday, "Tue" => DayOfWeek.Tuesday, "Wed" => DayOfWeek.Wednesday, "Thu" => DayOfWeek.Thursday,
            "Fri" => DayOfWeek.Friday, "Sat" => DayOfWeek.Saturday, _ => DayOfWeek.Sunday,
        };
    }

    /// <summary>
    /// Sends the streamed summary as say deltas, guarded (spec 8). It sends only whole words (up to the last
    /// whitespace, since a word may still grow into a banned term) and only while the text so far passes the copy guard.
    /// It never goes past the second sentence, so the stream shows no more than the summary. On a hit it stops for the
    /// rest of the stream, and the guarded summary event replaces what was shown.
    /// </summary>
    sealed class SayStream(Action<string> emit)
    {
        readonly StringBuilder _text = new();
        int _sent;
        bool _closed;

        public void Add(string delta)
        {
            _text.Append(delta);
            if (_closed) return;
            var all = _text.ToString();
            if (CopyGuard.Find(all).Count > 0)
            {
                _closed = true;
                return;
            }
            var cut = CopyGuard.TwoSentencesEnd(all);
            if (cut >= 0)
            {
                Send(all, cut);
                _closed = true;
                return;
            }
            Send(all, LastSpace(all));
        }

        /// <summary>The stream finished: the tail goes out only if the whole text still passes the guard.</summary>
        public void End()
        {
            if (_closed) return;
            _closed = true;
            var all = _text.ToString();
            if (CopyGuard.Find(all).Count == 0) Send(all, all.TrimEnd().Length);
        }

        /// <summary>Sends up to upTo when that prefix passes the guard on its own; otherwise holds it for later.</summary>
        void Send(string all, int upTo)
        {
            var from = _sent == 0 ? all.Length - all.TrimStart().Length : _sent; // no leading whitespace: the summary is trimmed
            if (upTo <= from || CopyGuard.Find(all[..upTo]).Count > 0) return;
            emit(all[from..upTo]);
            _sent = upTo;
        }

        static int LastSpace(string s)
        {
            for (var i = s.Length - 1; i >= 0; i--)
                if (char.IsWhiteSpace(s[i])) return i;
            return 0;
        }
    }
}
