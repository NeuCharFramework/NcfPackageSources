/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：EfLiveStore.cs
    文件功能描述：NeuCharBox Live 领域与数据库（Services）：EfLiveStore


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>Per-model totals for the Runs page. Runs counts every run, what-ifs included; WhatIfs is how many of them were.</summary>
public record RunTotals(string Model, int Runs, int WhatIfs, long PromptTokens, long CompletionTokens)
{
    public long Tokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// The invite, application and run stores in the NCF database (this module's three tables). A singleton (registered
/// by <c>UseStores&lt;EfLiveStore&gt;()</c>) that opens a service scope per call for its DbContext, so it is safe to
/// share. Times are stored as UTC <see cref="DateTime"/>s and come back as <see cref="DateTimeOffset"/>s at offset 0.
/// Single-row changes are one UPDATE statement each, so two requests touching the same code never lose a use.
/// </summary>
public class EfLiveStore(IServiceScopeFactory scopes) : IInviteStore, IApplicationStore, IRunStore
{
    // ---- IInviteStore ----

    public Task<InviteCode?> FindAsync(string normalized) => WithDb(async db =>
    {
        var e = await db.InviteCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Normalized == normalized);
        return e is null ? null : ToRecord(e);
    });

    public async Task<InviteCode> CreateAsync(string label, int dailyLimit)
    {
        for (var attempt = 1; ; attempt++)
        {
            var code = InviteCodes.Generate();
            var normalized = InviteCodes.Normalize(code);
            var made = await WithDb(async (db, now) =>
            {
                if (await db.InviteCodes.IgnoreQueryFilters().AnyAsync(c => c.Normalized == normalized)) return null;
                var e = new InviteCodeEntity
                {
                    Code = code, Normalized = normalized, Label = label, DailyLimit = dailyLimit,
                    AddTime = now.UtcDateTime, LastUpdateTime = now.UtcDateTime,
                };
                db.InviteCodes.Add(e);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException) when (attempt < 5)
                {
                    return null; // another call stored the same code between the check and the insert: draw again
                }
                return ToRecord(e);
            });
            if (made is not null) return made;
        }
    }

    /// <summary>Explicit: IApplicationStore and IRunStore have a ListAsync too.</summary>
    Task<IReadOnlyList<InviteCode>> IInviteStore.ListAsync() => WithDb<IReadOnlyList<InviteCode>>(async db =>
        (await db.InviteCodes.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync()).Select(ToRecord).ToList());

    public Task SetLimitAsync(long id, int n) => UpdateOne("invite code", id, (db, now, key) =>
        db.InviteCodes.Where(c => c.Id == key).ExecuteUpdateAsync(u => u
            .SetProperty(c => c.DailyLimit, n)
            .SetProperty(c => c.LastUpdateTime, now)));

    public Task SetDisabledAsync(long id, bool disabled) => UpdateOne("invite code", id, (db, now, key) =>
        db.InviteCodes.Where(c => c.Id == key).ExecuteUpdateAsync(u => u
            .SetProperty(c => c.Disabled, disabled)
            .SetProperty(c => c.LastUpdateTime, now)));

    public Task TouchAsync(long id, DateTimeOffset utc) => UpdateOne("invite code", id, (db, now, key) =>
        db.InviteCodes.Where(c => c.Id == key).ExecuteUpdateAsync(u => u
            .SetProperty(c => c.UsesTotal, c => c.UsesTotal + 1)
            .SetProperty(c => c.LastUsedUtc, utc.UtcDateTime)
            .SetProperty(c => c.LastUpdateTime, now)));

    // ---- IApplicationStore ----

    public Task<long> AddAsync(Application a) => WithDb(async (db, now) =>
    {
        var e = new ApplicationEntity
        {
            Email = a.Email, Name = a.Name, Interest = a.Interest, WouldTry = a.WouldTry, Scene = a.Scene,
            SubmittedUtc = a.Submitted.UtcDateTime, Status = a.Status, CodeId = ToKey(a.CodeId),
            AddTime = now.UtcDateTime, LastUpdateTime = now.UtcDateTime,
        };
        db.Applications.Add(e);
        await db.SaveChangesAsync();
        return (long)e.Id;
    });

    Task<IReadOnlyList<Application>> IApplicationStore.ListAsync() => WithDb<IReadOnlyList<Application>>(async db =>
        (await db.Applications.AsNoTracking().OrderByDescending(a => a.SubmittedUtc).ThenByDescending(a => a.Id).ToListAsync())
            .Select(ToRecord).ToList());

    public Task SetStatusAsync(long id, string status, long? codeId)
    {
        var code = ToKey(codeId);
        return UpdateOne("application", id, (db, now, key) =>
            db.Applications.Where(a => a.Id == key).ExecuteUpdateAsync(u => u
                .SetProperty(a => a.Status, status)
                .SetProperty(a => a.CodeId, code)
                .SetProperty(a => a.LastUpdateTime, now)));
    }

    public Task<bool> TryApproveAsync(long id, long codeId) => WithDb(async (db, now) =>
    {
        if (id is <= 0 or > int.MaxValue || codeId is <= 0 or > int.MaxValue) throw new KeyNotFoundException($"no application {id}");
        var key = (int)id;
        var code = (int)codeId;
        var n = await db.Applications.Where(a => a.Id == key && a.Status != "approved").ExecuteUpdateAsync(u => u
            .SetProperty(a => a.Status, "approved")
            .SetProperty(a => a.CodeId, code)
            .SetProperty(a => a.LastUpdateTime, now.UtcDateTime));
        if (n == 0 && !await db.Applications.AnyAsync(a => a.Id == key)) throw new KeyNotFoundException($"no application {id}");
        return n == 1;
    });

    // ---- IRunStore ----

    public Task<long> StartAsync(RunStart s) => WithDb(async (db, now) =>
    {
        var e = new RunEntity
        {
            Code = s.Code, Scene = s.Scene, Text = s.Text, WhatIf = s.WhatIf, WhatIfDevice = s.WhatIfDevice, Model = s.Model,
            Day = s.Day, StartedUtc = s.Started.UtcDateTime, AddTime = now.UtcDateTime, LastUpdateTime = now.UtcDateTime,
        };
        db.Runs.Add(e);
        await db.SaveChangesAsync();
        return (long)e.Id;
    });

    public Task FinishAsync(long runId, RunEnd e) => UpdateOne("run", runId, (db, now, key) =>
        db.Runs.Where(r => r.Id == key).ExecuteUpdateAsync(u => u
            .SetProperty(r => r.Outcome, e.Outcome)
            .SetProperty(r => r.PlanJson, e.PlanJson)
            .SetProperty(r => r.Summary, e.Summary)
            .SetProperty(r => r.Calls, e.Calls)
            .SetProperty(r => r.PromptTokens, e.PromptTokens)
            .SetProperty(r => r.CompletionTokens, e.CompletionTokens)
            .SetProperty(r => r.LatencyMs, e.LatencyMs)
            .SetProperty(r => r.FinishedUtc, e.Finished.UtcDateTime)
            .SetProperty(r => r.LastUpdateTime, now)));

    public Task<int> CountTodayAsync(DateOnly day, string? code)
    {
        var upper = code?.ToUpperInvariant();
        return WithDb(db => db.Runs.CountAsync(r => r.Day == day && !r.WhatIf && (upper == null || r.Code.ToUpper() == upper)));
    }

    public Task<long> TokensTodayAsync(DateOnly day) => WithDb(db =>
        db.Runs.Where(r => r.Day == day && r.FinishedUtc != null).SumAsync(r => (long)r.PromptTokens + r.CompletionTokens));

    public Task<IReadOnlyList<RunRow>> ListAsync(int take) => WithDb<IReadOnlyList<RunRow>>(async db =>
        (await db.Runs.AsNoTracking().OrderByDescending(r => r.Id).Take(Math.Max(0, take)).ToListAsync()).Select(ToRecord).ToList());

    /// <summary>One UPDATE over the old rows that still hold text; a row with no end yet is blanked the same way.</summary>
    public Task BlankTextOlderThanAsync(DateTimeOffset cutoff)
    {
        var before = cutoff.UtcDateTime;
        return WithDb(async (db, now) =>
        {
            await db.Runs
                .Where(r => r.StartedUtc < before && (r.Text != "" || r.PlanJson != null || r.Summary != null))
                .ExecuteUpdateAsync(u => u
                    .SetProperty(r => r.Text, "")
                    .SetProperty(r => r.PlanJson, (string?)null)
                    .SetProperty(r => r.Summary, (string?)null)
                    .SetProperty(r => r.LastUpdateTime, now.UtcDateTime));
            return 0;
        });
    }

    // ---- For the admin pages (not part of the store interfaces) ----

    /// <summary>Totals per model, for one usage day or (null) for all time, model ids in order.</summary>
    public Task<IReadOnlyList<RunTotals>> TotalsAsync(DateOnly? day) => WithDb<IReadOnlyList<RunTotals>>(async db =>
    {
        var runs = db.Runs.AsNoTracking();
        if (day is { } d) runs = runs.Where(r => r.Day == d);
        var rows = await runs
            .GroupBy(r => r.Model)
            .Select(g => new
            {
                Model = g.Key,
                Runs = g.Count(),
                WhatIfs = g.Count(r => r.WhatIf),
                Prompt = g.Sum(r => (long)r.PromptTokens),
                Completion = g.Sum(r => (long)r.CompletionTokens),
            })
            .ToListAsync();
        return rows.OrderBy(r => r.Model, StringComparer.Ordinal)
            .Select(r => new RunTotals(r.Model, r.Runs, r.WhatIfs, r.Prompt, r.Completion)).ToList();
    });

    /// <summary>Runs per code on that usage day, what-ifs excluded (as the daily limit counts them), keyed by code.</summary>
    public Task<IReadOnlyDictionary<string, int>> UsesOnAsync(DateOnly day) => WithDb<IReadOnlyDictionary<string, int>>(async db =>
    {
        var rows = await db.Runs.Where(r => r.Day == day && !r.WhatIf)
            .GroupBy(r => r.Code.ToUpper()).Select(g => new { Code = g.Key, N = g.Count() }).ToListAsync();
        return rows.ToDictionary(r => r.Code, r => r.N, StringComparer.OrdinalIgnoreCase);
    });

    // ---- helpers ----

    Task<T> WithDb<T>(Func<NeuCharBoxLiveSenparcEntities, Task<T>> f) => WithDb((db, _) => f(db));

    async Task<T> WithDb<T>(Func<NeuCharBoxLiveSenparcEntities, DateTimeOffset, Task<T>> f)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeuCharBoxLiveSenparcEntities>();
        var now = (scope.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow();
        return await f(db, now);
    }

    /// <summary>Runs a single-row ExecuteUpdate; like the in-memory store, an unknown id is a KeyNotFoundException.</summary>
    Task UpdateOne(string what, long id, Func<NeuCharBoxLiveSenparcEntities, DateTime, int, Task<int>> update) =>
        WithDb(async (db, now) =>
        {
            var n = id is > 0 and <= int.MaxValue ? await update(db, now.UtcDateTime, (int)id) : 0;
            if (n == 0) throw new KeyNotFoundException($"no {what} {id}");
            return n;
        });

    static int? ToKey(long? id) => id is null ? null : checked((int)id.Value);

    static DateTimeOffset Utc(DateTime t) => new(DateTime.SpecifyKind(t, DateTimeKind.Utc));

    static InviteCode ToRecord(InviteCodeEntity e) =>
        new(e.Id, e.Code, e.Label, Utc(e.AddTime), e.DailyLimit, e.Disabled, e.UsesTotal, e.LastUsedUtc is { } t ? Utc(t) : null);

    static Application ToRecord(ApplicationEntity e) =>
        new(e.Id, e.Email, e.Name, e.Interest, e.WouldTry, e.Scene, Utc(e.SubmittedUtc), e.Status, e.CodeId);

    static RunRow ToRecord(RunEntity e) => new(e.Id,
        new RunStart(e.Code, e.Scene, e.Text, e.WhatIf, e.WhatIfDevice, e.Model, e.Day, Utc(e.StartedUtc)),
        e.FinishedUtc is { } f
            ? new RunEnd(e.Outcome ?? "", e.PlanJson, e.Summary, e.Calls, e.PromptTokens, e.CompletionTokens, e.LatencyMs, Utc(f))
            : null);
}
