/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AccessService.cs
    文件功能描述：邀请码校验与每日限额的占用（每码与全局）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>Ok with the code and today's remaining live requests, or Error "bad_code".</summary>
public record AccessResult(bool Ok, InviteCode? Code, int Remaining, string? Error);

/// <summary>Error is a contract kind ("budget", "global_limit", "code_limit"), or null with the reserved run's id.</summary>
public record AdmitResult(string? Error, long? RunId);

/// <summary>
/// Invite codes and the daily limits (spec 7.1, global constraints). Days are usage days (<see cref="UsageDay"/>). The
/// limits are the runtime's current ones, unless a request passes its own snapshot's.
/// </summary>
public sealed class AccessService(IInviteStore codes, IRunStore runs, ITokenMeter meter, ILiveRuntime runtime, UsageDay usageDay,
    TimeProvider tp)
{
    static readonly AccessResult BadCode = new(false, null, 0, "bad_code");

    /// <summary>One lock for every session's admissions: AccessService is a singleton, and the counts are global.</summary>
    readonly SemaphoreSlim _admit = new(1, 1);

    /// <summary>
    /// Checks an Authorization header value ("Bearer NCB-…", any case) or a bare code. Unknown, disabled, missing or
    /// empty → bad_code. Remaining = max(0, min(code's DailyLimit − its runs today, GlobalDaily − all runs today)).
    /// </summary>
    public async Task<AccessResult> CheckAsync(string? bearerOrCode)
    {
        var normalized = InviteCodes.Normalize(StripBearer(bearerOrCode));
        if (normalized.Length == 0) return BadCode;
        var code = await codes.FindAsync(normalized);
        if (code is null || code.Disabled) return BadCode;
        var day = usageDay.For(tp.GetUtcNow());
        var mine = await runs.CountTodayAsync(day, code.Code);
        var all = await runs.CountTodayAsync(day, null);
        var global = runtime.Current.Limits.GlobalDaily;
        return new AccessResult(true, code, Math.Max(0, Math.Min(code.DailyLimit - mine, global - all)), null);
    }

    /// <summary>The value without a leading "Bearer" scheme (any case). Another scheme stays, so it can't match a code.</summary>
    static string StripBearer(string? v)
    {
        var s = (v ?? "").Trim();
        const string scheme = "bearer";
        if (s.Length > scheme.Length && s.StartsWith(scheme, StringComparison.OrdinalIgnoreCase) && char.IsWhiteSpace(s[scheme.Length]))
            return s[scheme.Length..];
        return string.Equals(s, scheme, StringComparison.OrdinalIgnoreCase) ? "" : s;
    }

    /// <summary>
    /// Admits a live request on <paramref name="c"/>: budget → global_limit → code_limit, against start.Day. An
    /// admitted request is reserved at once: <paramref name="start"/> is logged, and the code touched, before the next
    /// admission can count, so two tabs at 19 of 20 can't both get in (nor two codes at 299 of 300).
    /// </summary>
    /// <param name="limits">The request's snapshot's limits; null reads the runtime's current ones.</param>
    public async Task<AdmitResult> AdmitRequestAsync(InviteCode c, RunStart start, LiveLimits? limits = null)
    {
        limits ??= runtime.Current.Limits;
        // The caller checked the code (CheckAsync) and logs the run under it: anything else is a bug, not a visitor error.
        if (c.Disabled) throw new ArgumentException($"invite code {c.Id} is disabled", nameof(c));
        if (!string.Equals(start.Code, c.Code, StringComparison.Ordinal))
            throw new ArgumentException("the run must be logged under the code it is admitted on", nameof(start));
        await _admit.WaitAsync(CancellationToken.None);
        try
        {
            if (meter.Spent(start.Day, limits.DailyTokenBudget)) return new AdmitResult("budget", null);
            if (await runs.CountTodayAsync(start.Day, null) >= limits.GlobalDaily) return new AdmitResult("global_limit", null);
            if (await runs.CountTodayAsync(start.Day, c.Code) >= c.DailyLimit) return new AdmitResult("code_limit", null);
            await codes.TouchAsync(c.Id, start.Started); // first: if it throws, no slot is left reserved without a run
            return new AdmitResult(null, await runs.StartAsync(start));
        }
        finally { _admit.Release(); }
    }

    /// <summary>
    /// The kill switch for that usage day, against <paramref name="limits"/> (a request's snapshot's) or, when null, the
    /// runtime's current ones. A what-if skips admission but is still refused once it trips.
    /// </summary>
    public bool BudgetSpent(DateOnly day, LiveLimits? limits = null) =>
        limits is null ? meter.Spent(day) : meter.Spent(day, limits.DailyTokenBudget);
}
