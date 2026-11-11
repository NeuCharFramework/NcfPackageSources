/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveOptions.cs
    文件功能描述：引擎调参（NcbLive 配置段）及其问题检测


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 每个请求的调用数、工作时间、审批等待与会话空闲超时默认不设上限（0 表示不限），请求不再被中途截断

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>
/// The engine's tuning, bound from config section <see cref="Section"/>. On/off, the models, the daily limits and the
/// allowed origins are not here: the engine reads them per request from <see cref="ILiveRuntime"/>. Settings that can't
/// work together never stop the host: <see cref="Problems"/> names them, and live stays off until they are fixed.
/// </summary>
public class LiveOptions
{
    public const string Section = "NcbLive";

    /// <summary>"required" makes the planning call pick a tool; "auto" lets it answer in text (the runner then parses JSON).</summary>
    public string PlanToolMode { get; set; } = "required";

    /// <summary>Model calls per request; 0 (the default) or less is no limit: a request is never cut short for it.</summary>
    public int MaxCalls { get; set; }

    /// <summary>Model calls per what-if; 0 (the default) or less is no limit.</summary>
    public int MaxCallsWhatIf { get; set; }

    public int MaxChars { get; set; } = 400;

    /// <summary>Server work time per request, approval waits excluded; zero (the default) or less is no limit.</summary>
    public TimeSpan MaxWork { get; set; } = TimeSpan.Zero;

    public int MaxCompletionTokens { get; set; } = 1500;
    /// <summary>How long a plan waits for approval before it expires; zero (the default) or less waits for as long as it takes.</summary>
    public TimeSpan ApprovalWait { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Idle time after which a session is dropped; zero (the default) or less never drops one for being idle. Then a
    /// session leaves only when its code opens one too many, or, with the server at <see cref="MaxSessions"/>, as the
    /// least recently active idle one when a new session needs the place.
    /// </summary>
    public TimeSpan SessionIdle { get; set; } = TimeSpan.Zero;
    public int HistoryTurns { get; set; } = 6;
    public FastForwardPacing Pacing { get; set; } = new();

    /// <summary>Live sessions the server holds at once, all codes together; at the cap POST sessions is 429 global_limit.</summary>
    public int MaxSessions { get; set; } = 200;

    /// <summary>POST access/check calls per IP in any sliding minute; over it, 429 rate.</summary>
    public int AccessCheckPerMinute { get; set; } = 30;

    /// <summary>The time zone whose midnight resets the daily limits and the token budget (<see cref="UsageDay.TryFromId"/>).</summary>
    public string UsageTimeZone { get; set; } = "UTC";

    /// <summary>
    /// The names of the settings that can't work (as they are, or together); empty when all is well. Never throws.
    /// </summary>
    public IReadOnlyList<string> Problems()
    {
        var p = new List<string>();
        // A request can be quiet for its work time plus one approval wait; a session with an idle timeout must outlive
        // that, or it could expire while its request waits for approval. With either of the two unlimited, no idle
        // timeout is long enough.
        if (SessionIdle > TimeSpan.Zero && (MaxWork <= TimeSpan.Zero || ApprovalWait <= TimeSpan.Zero || SessionIdle < MaxWork + ApprovalWait))
            p.Add(nameof(SessionIdle));
        if (MaxSessions < 1) p.Add(nameof(MaxSessions));               // no session could open
        if (AccessCheckPerMinute < 1) p.Add(nameof(AccessCheckPerMinute)); // no invite code could be checked
        if (!UsageDay.TryFromId(UsageTimeZone, out _)) p.Add(nameof(UsageTimeZone));
        return p;
    }
}
