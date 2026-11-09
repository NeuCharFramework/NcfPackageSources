/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveSettingsService.cs
    文件功能描述：NeuCharBox Live 领域服务：LiveSettingsService


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增设置读取与保存服务

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>What the admin can set. Model ids are AI Kernel model Ids; null = not chosen.</summary>
public sealed record LiveSettings(bool Enabled, int? PlanModelId, int? RunModelId, int PerCodeDaily, int GlobalDaily,
    long DailyTokenBudget, IReadOnlyList<string> ExtraOrigins)
{
    public static LiveSettings Defaults { get; } = new(true, null, null, 20, 300, 1_000_000, []);

    public LiveLimits Limits => new(PerCodeDaily, GlobalDaily, DailyTokenBudget);
}

/// <summary>
/// Reads and saves the settings row (this module's table <c>Senparc_NeuCharBoxLive_Settings</c>). A singleton that opens a
/// service scope per call for its DbContext, like <see cref="EfLiveStore"/>. No row means <see cref="LiveSettings.Defaults"/>.
/// </summary>
public sealed class LiveSettingsService(IServiceScopeFactory scopes)
{
    /// <summary>Raised after a successful save.</summary>
    public event Action? Changed;

    public async Task<LiveSettings> GetAsync(CancellationToken ct = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NeuCharBoxLiveSenparcEntities>();
        var e = await db.Settings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(ct);
        return e is null ? LiveSettings.Defaults : ToSettings(e);
    }

    /// <summary>
    /// Saves <paramref name="s"/> (origins stored normalized) and raises <see cref="Changed"/>, or, when
    /// <see cref="Validate"/> finds problems, returns their property names and changes nothing.
    /// </summary>
    public async Task<IReadOnlyList<string>> SaveAsync(LiveSettings s, CancellationToken ct = default)
    {
        var problems = Validate(s);
        if (problems.Count > 0) return problems;

        await using (var scope = scopes.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NeuCharBoxLiveSenparcEntities>();
            var now = (scope.ServiceProvider.GetService<TimeProvider>() ?? TimeProvider.System).GetUtcNow().UtcDateTime;
            var e = await db.Settings.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
            if (e is null)
            {
                e = new SettingsEntity { AddTime = now };
                db.Settings.Add(e);
            }
            e.Enabled = s.Enabled;
            e.PlanModelId = s.PlanModelId;
            e.RunModelId = s.RunModelId;
            e.PerCodeDaily = s.PerCodeDaily;
            e.GlobalDaily = s.GlobalDaily;
            e.DailyTokenBudget = s.DailyTokenBudget;
            e.ExtraOrigins = string.Join('\n', s.ExtraOrigins.Select(Normalize));
            e.UpdatedUtc = now;
            e.LastUpdateTime = now;
            await db.SaveChangesAsync(ct);
        }

        Changed?.Invoke();
        return problems;
    }

    /// <summary>The names of the invalid properties: the three limits must be at least 1 and every extra origin must pass <see cref="LiveCors.TryNormalize"/>.</summary>
    public static IReadOnlyList<string> Validate(LiveSettings s)
    {
        var problems = new List<string>();
        if (s.PerCodeDaily < 1) problems.Add(nameof(LiveSettings.PerCodeDaily));
        if (s.GlobalDaily < 1) problems.Add(nameof(LiveSettings.GlobalDaily));
        if (s.DailyTokenBudget < 1) problems.Add(nameof(LiveSettings.DailyTokenBudget));
        if ((s.ExtraOrigins ?? []).Any(o => !LiveCors.TryNormalize(o, out _))) problems.Add(nameof(LiveSettings.ExtraOrigins));
        return problems;
    }

    static string Normalize(string raw)
    {
        LiveCors.TryNormalize(raw, out var origin);
        return origin;
    }

    static LiveSettings ToSettings(SettingsEntity e) => new(e.Enabled, e.PlanModelId, e.RunModelId, e.PerCodeDaily, e.GlobalDaily,
        e.DailyTokenBudget,
        e.ExtraOrigins.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
