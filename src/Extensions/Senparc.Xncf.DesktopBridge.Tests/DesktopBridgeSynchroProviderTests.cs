/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeSynchroProviderTests.cs
    文件功能描述：DesktopBridge Synchro Provider 测试

    创建标识：Senparc - 20260802
----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Shared.Abstractions.Synchro;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Tests;

[TestClass]
public class DesktopBridgeSynchroProviderTests
{
    [TestMethod]
    public async Task GetSnapshotAsync_ShouldExposePendingPairingCount()
    {
        var store = new DesktopBridgeCredentialStore(legacyToken: null);
        var provider = new DesktopBridgeSynchroProvider(store);

        var emptySnapshot = await provider.GetSnapshotAsync(new SynchroRequestContext("admin"));
        Assert.AreEqual(Register.ModuleUid, provider.ModuleUid);
        Assert.AreEqual(0, emptySnapshot.Items.Single().Count);

        store.CreatePairingRequest("Desktop Test", "127.0.0.1");
        var pendingSnapshot = await provider.GetSnapshotAsync(new SynchroRequestContext("admin"));

        Assert.AreEqual(DesktopBridgeSynchroProvider.ProviderIdValue, pendingSnapshot.ProviderId);
        Assert.AreEqual(1, pendingSnapshot.Items.Single().Count);
        Assert.AreEqual("warning", pendingSnapshot.Items.Single().Severity);
    }
}
