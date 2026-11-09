/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：EfLiveStoreTests.cs
    文件功能描述：NeuCharBox Live 测试：EfLiveStoreTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

    修改标识：Senparc - 20261108
    修改描述：v0.2.0-preview1 TestDb 暴露 Scopes，供 LiveSettingsService 测试使用

----------------------------------------------------------------*/

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Tests;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Models;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// The shared store contract (StoreContract.cs, in this project) against EfLiveStore on the module's real
/// SQLite DbContext, each test on its own in-memory database built with EnsureCreated().
/// </summary>
[TestClass]
public class EfLiveStoreTests
{
    [TestMethod] public async Task Invites() { using var db = new TestDb(); await StoreContract.Invites(db.Store); }

    [TestMethod] public async Task Applications() { using var db = new TestDb(); await StoreContract.Applications(db.Store); }

    [TestMethod] public async Task TryApprove() { using var db = new TestDb(); await StoreContract.TryApprove(db.Store); }

    [TestMethod] public async Task Runs() { using var db = new TestDb(); await StoreContract.Runs(db.Store); }

    [TestMethod] public async Task Retention() { using var db = new TestDb(); await StoreContract.Retention(db.Store); }
}

/// <summary>
/// One in-memory SQLite database (it lives as long as the open connection) and a service provider whose scopes give
/// NeuCharBoxLiveSenparcEntities, as the host's do; <see cref="Store"/> is an EfLiveStore on it.
/// </summary>
sealed class TestDb : IDisposable
{
    readonly SqliteConnection _connection = new("DataSource=:memory:");
    readonly ServiceProvider _provider;

    public EfLiveStore Store { get; }

    public IServiceScopeFactory Scopes => _provider.GetRequiredService<IServiceScopeFactory>();

    public TestDb(bool create = true)
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<NeuCharBoxLiveSenparcEntities_Sqlite>().UseSqlite(_connection).Options;
        var services = new ServiceCollection();
        services.AddScoped<NeuCharBoxLiveSenparcEntities>(_ => new NeuCharBoxLiveSenparcEntities_Sqlite(options));
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        if (create)
            using (var db = NewContext())
                db.Database.EnsureCreated();
        Store = new EfLiveStore(_provider.GetRequiredService<IServiceScopeFactory>());
    }

    /// <summary>A new context on the same database; the caller disposes it.</summary>
    public NeuCharBoxLiveSenparcEntities_Sqlite NewContext() =>
        new(new DbContextOptionsBuilder<NeuCharBoxLiveSenparcEntities_Sqlite>().UseSqlite(_connection).Options);

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
