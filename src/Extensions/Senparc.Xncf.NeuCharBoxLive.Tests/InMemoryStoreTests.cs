/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：InMemoryStoreTests.cs
    文件功能描述：测试内存存储（InMemoryLiveStore）是否满足存储契约


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>The shared store contract against a new InMemoryLiveStore each time.</summary>
[TestClass]
public class InMemoryStoreTests
{
    [TestMethod] public Task Invites() => StoreContract.Invites(new InMemoryLiveStore());

    [TestMethod] public Task Applications() => StoreContract.Applications(new InMemoryLiveStore());

    [TestMethod] public Task TryApprove() => StoreContract.TryApprove(new InMemoryLiveStore());

    [TestMethod] public Task Runs() => StoreContract.Runs(new InMemoryLiveStore());

    [TestMethod] public Task Retention() => StoreContract.Retention(new InMemoryLiveStore());
}
