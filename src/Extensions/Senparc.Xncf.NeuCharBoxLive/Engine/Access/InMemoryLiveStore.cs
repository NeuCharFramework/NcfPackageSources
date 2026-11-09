/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：InMemoryLiveStore.cs
    文件功能描述：NeuCharBox Live 引擎（Access）：InMemoryLiveStore


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Access;

/// <summary>In-memory stores for tests and a module-less host: invite codes, applications and the run log.</summary>
public class InMemoryLiveStore : IInviteStore, IApplicationStore, IRunStore
{
    readonly object _gate = new();
    readonly TimeProvider _tp;
    readonly List<RunRow> _runs = new();
    readonly List<InviteCode> _codes = new();
    readonly List<Application> _applications = new();

    /// <param name="tp">Stamps a new code's Created; the system clock when null.</param>
    public InMemoryLiveStore(TimeProvider? tp = null) => _tp = tp ?? TimeProvider.System;

    // ---- IInviteStore ----

    public Task<InviteCode?> FindAsync(string normalized)
    {
        lock (_gate)
            return Task.FromResult(_codes.FirstOrDefault(c => InviteCodes.Normalize(c.Code) == normalized));
    }

    public Task<InviteCode> CreateAsync(string label, int dailyLimit)
    {
        lock (_gate)
        {
            string code;
            do code = InviteCodes.Generate();
            while (_codes.Any(c => InviteCodes.Normalize(c.Code) == InviteCodes.Normalize(code)));
            var created = new InviteCode(_codes.Count + 1L, code, label, _tp.GetUtcNow(), dailyLimit, false, 0, null);
            _codes.Add(created);
            return Task.FromResult(created);
        }
    }

    /// <summary>Explicit: IApplicationStore and IRunStore have a ListAsync too.</summary>
    Task<IReadOnlyList<InviteCode>> IInviteStore.ListAsync()
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<InviteCode>>(_codes.OrderByDescending(c => c.Id).ToList());
    }

    public Task SetLimitAsync(long id, int n) => UpdateCode(id, c => c with { DailyLimit = n });

    public Task SetDisabledAsync(long id, bool disabled) => UpdateCode(id, c => c with { Disabled = disabled });

    public Task TouchAsync(long id, DateTimeOffset utc) => UpdateCode(id, c => c with { UsesTotal = c.UsesTotal + 1, LastUsed = utc });

    Task UpdateCode(long id, Func<InviteCode, InviteCode> change)
    {
        lock (_gate)
        {
            var i = _codes.FindIndex(c => c.Id == id);
            if (i < 0) throw new KeyNotFoundException($"no invite code {id}");
            _codes[i] = change(_codes[i]);
        }
        return Task.CompletedTask;
    }

    // ---- IApplicationStore ----

    public Task<long> AddAsync(Application a)
    {
        lock (_gate)
        {
            var id = _applications.Count + 1L;
            _applications.Add(a with { Id = id });
            return Task.FromResult(id);
        }
    }

    Task<IReadOnlyList<Application>> IApplicationStore.ListAsync()
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<Application>>(
                _applications.OrderByDescending(a => a.Submitted).ThenByDescending(a => a.Id).ToList());
    }

    public Task SetStatusAsync(long id, string status, long? codeId)
    {
        lock (_gate)
        {
            var i = _applications.FindIndex(a => a.Id == id);
            if (i < 0) throw new KeyNotFoundException($"no application {id}");
            _applications[i] = _applications[i] with { Status = status, CodeId = codeId };
        }
        return Task.CompletedTask;
    }

    public Task<bool> TryApproveAsync(long id, long codeId)
    {
        lock (_gate)
        {
            var i = _applications.FindIndex(a => a.Id == id);
            if (i < 0) throw new KeyNotFoundException($"no application {id}");
            if (_applications[i].Status == "approved") return Task.FromResult(false);
            _applications[i] = _applications[i] with { Status = "approved", CodeId = codeId };
            return Task.FromResult(true);
        }
    }

    // ---- IRunStore ----

    public Task<long> StartAsync(RunStart s)
    {
        lock (_gate)
        {
            var id = _runs.Count + 1L;
            _runs.Add(new RunRow(id, s, null));
            return Task.FromResult(id);
        }
    }

    public Task FinishAsync(long runId, RunEnd e)
    {
        lock (_gate)
        {
            var i = _runs.FindIndex(r => r.Id == runId);
            if (i < 0) throw new KeyNotFoundException($"no run {runId}");
            _runs[i] = _runs[i] with { End = e };
        }
        return Task.CompletedTask;
    }

    public Task<int> CountTodayAsync(DateOnly day, string? code)
    {
        lock (_gate)
            return Task.FromResult(_runs.Count(r => r.Start.Day == day && !r.Start.WhatIf
                && (code is null || string.Equals(r.Start.Code, code, StringComparison.OrdinalIgnoreCase))));
    }

    public Task<long> TokensTodayAsync(DateOnly day)
    {
        lock (_gate)
            return Task.FromResult(_runs.Where(r => r.Start.Day == day && r.End is not null)
                .Sum(r => (long)r.End!.PromptTokens + r.End.CompletionTokens));
    }

    public Task<IReadOnlyList<RunRow>> ListAsync(int take)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<RunRow>>(_runs.OrderByDescending(r => r.Id).Take(Math.Max(0, take)).ToList());
    }

    /// <summary>RunStart.Text is not nullable, so it is blanked to "", while PlanJson and Summary become null.</summary>
    public Task BlankTextOlderThanAsync(DateTimeOffset cutoff)
    {
        lock (_gate)
            for (var i = 0; i < _runs.Count; i++)
                if (_runs[i].Start.Started < cutoff)
                    _runs[i] = _runs[i] with
                    {
                        Start = _runs[i].Start with { Text = "" },
                        End = _runs[i].End is { } e ? e with { PlanJson = null, Summary = null } : null,
                    };
        return Task.CompletedTask;
    }
}
