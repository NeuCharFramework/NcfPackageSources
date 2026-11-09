/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveOptions.cs
    文件功能描述：引擎调参（NcbLive 配置段）及其问题检测


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

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

    public int MaxCalls { get; set; } = 60;
    public int MaxCallsWhatIf { get; set; } = 60;
    public int MaxChars { get; set; } = 400;

    /// <summary>Server work time per request, approval waits excluded.</summary>
    public TimeSpan MaxWork { get; set; } = TimeSpan.FromMinutes(30);

    public int MaxCompletionTokens { get; set; } = 1500;
    public TimeSpan ApprovalWait { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan SessionIdle { get; set; } = TimeSpan.FromMinutes(45);
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
        // A request can be quiet for its work time plus one approval wait; its session must outlive that, or a session
        // could expire while its request waits for approval.
        if (SessionIdle < MaxWork + ApprovalWait) p.Add(nameof(SessionIdle));
        if (MaxSessions < 1) p.Add(nameof(MaxSessions));               // no session could open
        if (AccessCheckPerMinute < 1) p.Add(nameof(AccessCheckPerMinute)); // no invite code could be checked
        if (!UsageDay.TryFromId(UsageTimeZone, out _)) p.Add(nameof(UsageTimeZone));
        return p;
    }
}
