/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Stores.cs
    文件功能描述：NeuCharBox Live 引擎（Access）：Stores


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>An invite code. Declared here because LiveSession holds it; Task 9's invite store returns it.</summary>
public record InviteCode(long Id, string Code, string Label, DateTimeOffset Created, int DailyLimit, bool Disabled, int UsesTotal, DateTimeOffset? LastUsed);

/// <summary>Day is the usage day (America/New_York) the run counts against.</summary>
public record RunStart(string Code, string Scene, string Text, bool WhatIf, string? WhatIfDevice, string Model, DateOnly Day, DateTimeOffset Started);

public record RunEnd(string Outcome, string? PlanJson, string? Summary, int Calls, int PromptTokens, int CompletionTokens, long LatencyMs, DateTimeOffset Finished);

/// <summary>End is null while the run is still going.</summary>
public record RunRow(long Id, RunStart Start, RunEnd? End);

/// <summary>The run log: one row per live request, what-ifs included.</summary>
public interface IRunStore
{
    Task<long> StartAsync(RunStart s);
    Task FinishAsync(long runId, RunEnd e);

    /// <summary>Runs started on that day, what-ifs excluded. A null code counts every code.</summary>
    Task<int> CountTodayAsync(DateOnly day, string? code);

    /// <summary>Prompt plus completion tokens of the runs that day, what-ifs included.</summary>
    Task<long> TokensTodayAsync(DateOnly day);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<RunRow>> ListAsync(int take);

    /// <summary>Blanks the visitor text (request text, plan, summary) of runs started before cutoff; numbers are kept.</summary>
    Task BlankTextOlderThanAsync(DateTimeOffset cutoff);
}

/// <summary>An application from the demo's apply form (spec 7.2). Status is "new", "approved" or "declined"; CodeId is the code issued.</summary>
public record Application(long Id, string Email, string? Name, string Interest, string WouldTry, string Scene, DateTimeOffset Submitted, string Status, long? CodeId);

/// <summary>The invite codes (spec 7.1). Codes are looked up by <see cref="InviteCodes.Normalize"/>'s form.</summary>
public interface IInviteStore
{
    /// <summary>The code whose normalized form is <paramref name="normalized"/>, disabled ones included; null when none.</summary>
    Task<InviteCode?> FindAsync(string normalized);

    /// <summary>A new enabled, unused code from <see cref="InviteCodes.Generate"/>, unique among the stored codes.</summary>
    Task<InviteCode> CreateAsync(string label, int dailyLimit);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<InviteCode>> ListAsync();

    Task SetLimitAsync(long id, int n);
    Task SetDisabledAsync(long id, bool disabled);

    /// <summary>A request was admitted on the code: UsesTotal goes up by one and LastUsed becomes <paramref name="utc"/>.</summary>
    Task TouchAsync(long id, DateTimeOffset utc);
}

/// <summary>The applications Leo reads in the admin (spec 7.2).</summary>
public interface IApplicationStore
{
    /// <summary>Stores the application under a new id (a.Id is ignored) and returns that id.</summary>
    Task<long> AddAsync(Application a);

    /// <summary>Newest first.</summary>
    Task<IReadOnlyList<Application>> ListAsync();

    /// <summary>Sets the status and the code issued (stored as given; null for none).</summary>
    Task SetStatusAsync(long id, string status, long? codeId);

    /// <summary>
    /// Approves the application with this code only if it is not approved yet (one conditional update). True if it
    /// changed; false if it was already approved, so the caller's code is spare. Unknown id: KeyNotFoundException.
    /// </summary>
    Task<bool> TryApproveAsync(long id, long codeId);
}

/// <summary>The daily token kill switch. Task 9 writes TokenMeter.</summary>
public interface ITokenMeter
{
    bool Spent(DateOnly day);
    void Add(DateOnly day, int tokens);
}
