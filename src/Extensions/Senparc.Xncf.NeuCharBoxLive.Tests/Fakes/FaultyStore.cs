/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FaultyStore.cs
    文件功能描述：NeuCharBox Live 测试：FaultyStore


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;

/// <summary>Which store member throws what. Registered per host, so parallel tests don't share it.</summary>
public sealed class Faults
{
    public Func<string, Exception?> On { get; set; } = _ => null;

    public void Check(string member)
    {
        if (On(member) is { } e) throw e;
    }
}

/// <summary>InMemoryLiveStore behind the three store interfaces, with any member made to throw (a store that broke).</summary>
public sealed class FaultyStore(Faults faults, TimeProvider tp) : IInviteStore, IApplicationStore, IRunStore
{
    public InMemoryLiveStore Inner { get; } = new(tp);

    public Task<InviteCode?> FindAsync(string normalized) { faults.Check(nameof(FindAsync)); return Inner.FindAsync(normalized); }
    public Task<InviteCode> CreateAsync(string label, int dailyLimit) { faults.Check(nameof(CreateAsync)); return Inner.CreateAsync(label, dailyLimit); }
    Task<IReadOnlyList<InviteCode>> IInviteStore.ListAsync() => ((IInviteStore)Inner).ListAsync();
    public Task SetLimitAsync(long id, int n) => Inner.SetLimitAsync(id, n);
    public Task SetDisabledAsync(long id, bool disabled) => Inner.SetDisabledAsync(id, disabled);
    public Task TouchAsync(long id, DateTimeOffset utc) { faults.Check(nameof(TouchAsync)); return Inner.TouchAsync(id, utc); }

    public Task<long> AddAsync(Application a) { faults.Check(nameof(AddAsync)); return Inner.AddAsync(a); }
    Task<IReadOnlyList<Application>> IApplicationStore.ListAsync() => ((IApplicationStore)Inner).ListAsync();
    public Task SetStatusAsync(long id, string status, long? codeId) => Inner.SetStatusAsync(id, status, codeId);
    public Task<bool> TryApproveAsync(long id, long codeId) => Inner.TryApproveAsync(id, codeId);

    public Task<long> StartAsync(RunStart s) { faults.Check(nameof(StartAsync)); return Inner.StartAsync(s); }
    public Task FinishAsync(long runId, RunEnd e) => Inner.FinishAsync(runId, e);
    public Task<int> CountTodayAsync(DateOnly day, string? code) { faults.Check(nameof(CountTodayAsync)); return Inner.CountTodayAsync(day, code); }
    public Task<long> TokensTodayAsync(DateOnly day) { faults.Check(nameof(TokensTodayAsync)); return Inner.TokensTodayAsync(day); }
    public Task<IReadOnlyList<RunRow>> ListAsync(int take) => Inner.ListAsync(take);
    public Task BlankTextOlderThanAsync(DateTimeOffset cutoff) => Inner.BlankTextOlderThanAsync(cutoff);
}
