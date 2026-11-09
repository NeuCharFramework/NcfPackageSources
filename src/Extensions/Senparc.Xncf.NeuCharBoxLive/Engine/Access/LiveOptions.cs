/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveOptions.cs
    文件功能描述：NeuCharBox Live 引擎（Access）：LiveOptions


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Sim;

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>
/// The live server's limits and models, bound from config section <see cref="Section"/>; Task 12 writes these
/// defaults into appsettings.json. The NeuChar key is never here: it comes from NEUCHAR_API_KEY only.
/// </summary>
public class LiveOptions
{
    public const string Section = "NcbLive";

    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = "https://www.neuchar.com/19194/";
    public string PlanModel { get; set; } = "gpt-5-chat";
    public string RunModel { get; set; } = "gpt-5-chat";

    /// <summary>"required" makes the planning call pick a tool; "auto" lets it answer in text (the runner then parses JSON).</summary>
    public string PlanToolMode { get; set; } = "required";

    public int PerCodeDaily { get; set; } = 20;
    public int GlobalDaily { get; set; } = 300;
    public int MaxCalls { get; set; } = 12;
    public int MaxCallsWhatIf { get; set; } = 14;
    public int MaxChars { get; set; } = 400;

    /// <summary>Server work time per request, approval waits excluded.</summary>
    public TimeSpan MaxWork { get; set; } = TimeSpan.FromMinutes(3);

    public int MaxCompletionTokens { get; set; } = 1500;
    public TimeSpan ApprovalWait { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan SessionIdle { get; set; } = TimeSpan.FromMinutes(30);
    public int HistoryTurns { get; set; } = 6;
    public long DailyTokenBudget { get; set; } = 1_000_000;
    public string[] ExtraOrigins { get; set; } = Array.Empty<string>();
    public FastForwardPacing Pacing { get; set; } = new();

    /// <summary>Live sessions the server holds at once, all codes together; at the cap POST sessions is 429 global_limit.</summary>
    public int MaxSessions { get; set; } = 200;

    /// <summary>POST access/check calls per IP in any sliding minute; over it, 429 rate.</summary>
    public int AccessCheckPerMinute { get; set; } = 30;

    /// <summary>
    /// Throws InvalidOperationException for settings that can't work together. SessionManager calls it when it is
    /// built, at startup.
    /// </summary>
    public void Validate()
    {
        // A request can be quiet for its work time plus one approval wait; its session must outlive that.
        if (SessionIdle < MaxWork + ApprovalWait)
            throw new InvalidOperationException(
                $"{Section}:SessionIdle ({SessionIdle}) must be at least {Section}:MaxWork + {Section}:ApprovalWait " +
                $"({MaxWork + ApprovalWait}), or a session could expire while its request waits for approval.");
        if (MaxSessions < 1)
            throw new InvalidOperationException($"{Section}:MaxSessions ({MaxSessions}) must be at least 1, or no session can open.");
        if (AccessCheckPerMinute < 1)
            throw new InvalidOperationException(
                $"{Section}:AccessCheckPerMinute ({AccessCheckPerMinute}) must be at least 1, or no invite code can be checked.");
    }
}
