/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveSession.cs
    文件功能描述：一个 Live 会话：房间、对话轮次、事件日志、请求与过期时间


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Engine.Events;
using Senparc.Xncf.NeuCharBoxLive.Engine.Scenes;
using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

/// <summary>One turn of the conversation the model sees on the next request.</summary>
public record Turn(string User, string Assistant);

/// <summary>
/// A visitor's live session: the room, its event log and the conversation. One request runs at a time
/// (<see cref="Active"/>); session-level changes (starting or finishing a request, reset) happen under <see cref="Gate"/>.
/// </summary>
public class LiveSession
{
    readonly object _lock = new();
    readonly Dictionary<string, RequestRecord> _requests = new();
    DateTimeOffset _lastActivity;
    RequestRecord? _active, _lastFinished, _lastReplayable;

    public LiveSession(string id, InviteCode code, SceneManifest scene, EventLog events, DateTimeOffset now)
    {
        Id = id;
        Code = code;
        Scene = scene;
        Events = events;
        PostSetup = RoomState.PostSetup(scene);
        Room = PostSetup.Clone();
        _lastActivity = now;
        _behavior = new Lazy<ISceneBehavior>(() => ManifestLoader.Behavior(scene));
    }

    readonly Lazy<ISceneBehavior> _behavior;

    /// <summary>The scene's behaviour (stateless, so one is shared by everyone who reads it): labels for the automations list.</summary>
    public ISceneBehavior Behavior => _behavior.Value;

    /// <summary>Where session-level problems that must not fail an edit or a run are logged (an automation card's text).</summary>
    public ILogger Log { get; init; } = NullLogger.Instance;

    readonly HashSet<string> _textFailures = new(StringComparer.Ordinal);

    /// <summary>True the first time this automation's card text failed, so it is logged once, not at every step.</summary>
    public bool FirstTextFailure(string id)
    {
        lock (_lock) return _textFailures.Add(id);
    }

    public string Id { get; }

    /// <summary>The whole invite code, so the runner sees the code's own DailyLimit for done.remainingToday.</summary>
    public InviteCode Code { get; set; }

    public SceneManifest Scene { get; }
    public RoomState Room { get; set; }

    /// <summary>The room right after setup; "Reset room" goes back to a copy of it.</summary>
    public RoomState PostSetup { get; }

    public EventLog Events { get; }

    /// <summary>The last few turns, oldest first. Changed under Gate only.</summary>
    public List<Turn> History { get; } = new();

    public RequestRecord? Active { get { lock (_lock) return _active; } set { lock (_lock) _active = value; } }
    public RequestRecord? LastFinished { get { lock (_lock) return _lastFinished; } set { lock (_lock) _lastFinished = value; } }
    public DateTimeOffset LastActivity { get { lock (_lock) return _lastActivity; } set { lock (_lock) _lastActivity = value; } }

    /// <summary>
    /// The what-if target (contract, client branch review): the latest non-what-if request that ended ok or failed after
    /// an approved plan. A later declined, cannot_do, expired or limit request, or a planning-only failure, leaves it.
    /// A reset clears it (<see cref="ClearReplayable"/>).
    /// </summary>
    public RequestRecord? LastReplayable { get { lock (_lock) return _lastReplayable; } }

    /// <summary>Nothing is replayable until the next request ends ok or failed (contract: a what-if after a reset is not_waiting). Call under Gate.</summary>
    public void ClearReplayable()
    {
        lock (_lock) _lastReplayable = null;
    }

    public SemaphoreSlim Gate { get; } = new(1, 1);

    /// <summary>Hand edits waiting for whoever owns the room.</summary>
    public EditQueue Edits { get; } = new();

    bool _advancing;
    readonly Dictionary<string, long> _editStamps = new();

    /// <summary>A clock advance is playing.</summary>
    public bool Advancing { get { lock (_lock) return _advancing; } set { lock (_lock) _advancing = value; } }

    /// <summary>A request or a clock advance owns the room, so an edit waits in <see cref="Edits"/>.</summary>
    public bool Busy { get { lock (_lock) return _active is not null || _advancing; } }

    /// <summary>
    /// How many times an edit or the request runner has claimed that path's newest frame. A frame stamped before (an
    /// older edit's delayed emit, the AI's later frame of an action) is dropped when this moved.
    /// </summary>
    public long EditStamp(string path)
    {
        lock (_lock) return _editStamps.GetValueOrDefault(path);
    }

    /// <summary>
    /// Held around "bump stamps + append" (ApplyAll, the runner's state events) and "check stamp + append" (a delayed
    /// emit, an action's paced frames), so the last state event for a path is always the newest writer's. Never held
    /// while taking Gate; takes only leaf locks inside.
    /// </summary>
    public object EmitGate { get; } = new();

    long _epoch;

    /// <summary>Changes on every <see cref="InvalidateDelayedEdits"/>. Read and written under <see cref="EmitGate"/>.</summary>
    public long Epoch => _epoch;

    /// <summary>A reset: every delayed emit still waiting is dropped.</summary>
    public void InvalidateDelayedEdits()
    {
        lock (EmitGate) _epoch++;
    }

    string _automations = "[]";
    HashSet<string> _automationIds = new(StringComparer.Ordinal);

    /// <summary>The JSON of the last automations list streamed (or "[]"). Read and written under <see cref="EmitGate"/>.</summary>
    public string LastAutomations => _automations;

    /// <summary>Records the list just streamed. Call under <see cref="EmitGate"/>, in one step with its append.</summary>
    public void SetLastAutomations(string json, IEnumerable<string> ids)
    {
        _automations = json;
        _automationIds = new HashSet<string>(ids, StringComparer.Ordinal);
    }

    /// <summary>
    /// The id is in the last automations list streamed: what the visitor can see, and so toggle. Every change to the
    /// list streams one, so a route can check an id without owning the room. Takes EmitGate: never call it holding a leaf lock.
    /// </summary>
    public bool KnowsAutomation(string id)
    {
        lock (EmitGate) return _automationIds.Contains(id);
    }

    /// <summary>Counts one more edit of that path and returns the new stamp.</summary>
    public long BumpEditStamp(string path)
    {
        lock (_lock) return _editStamps[path] = _editStamps.GetValueOrDefault(path) + 1;
    }

    /// <summary>Remembers the request, so approve and decline can find it by rid.</summary>
    public void Track(RequestRecord r)
    {
        lock (_lock) _requests[r.Rid] = r;
    }

    /// <summary>Tracks the request and makes it the active one. Call under Gate.</summary>
    public void Begin(RequestRecord r)
    {
        lock (_lock)
        {
            _requests[r.Rid] = r;
            _active = r;
        }
    }

    /// <summary>
    /// The request is over (set its Outcome first). A what-if never becomes LastFinished or LastReplayable (contract
    /// server note), so a second what-if on the same rid replays the same request. Call under Gate.
    /// </summary>
    public void End(RequestRecord r)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_active, r)) _active = null;
            if (r.IsWhatIf) return;
            _lastFinished = r;
            if (r.Plan is not null && r.Outcome is ("ok" or "failed")) _lastReplayable = r;
        }
    }

    /// <summary>Keeps the last <paramref name="max"/> turns. Call under Gate.</summary>
    public void AddTurn(Turn t, int max)
    {
        History.Add(t);
        if (History.Count > max) History.RemoveRange(0, History.Count - Math.Max(0, max));
    }

    /// <summary>
    /// Answers the card waiting on that request. Null when the answer was taken; otherwise the contract kind:
    /// "no_request" when this session never had that rid, "not_waiting" when nothing is waiting for an answer.
    /// </summary>
    public string? TryDecide(string rid, Decision d)
    {
        RequestRecord? r;
        lock (_lock) _requests.TryGetValue(rid, out r);
        if (r is null) return "no_request";
        return r.TryDecide(d) ? null : "not_waiting";
    }

    /// <summary>The request with that rid, if this session ever had it.</summary>
    public RequestRecord? Find(string rid)
    {
        lock (_lock) return _requests.GetValueOrDefault(rid);
    }
}
