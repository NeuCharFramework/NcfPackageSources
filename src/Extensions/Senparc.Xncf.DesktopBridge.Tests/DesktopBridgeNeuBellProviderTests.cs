/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: DesktopBridgeNeuBellProviderTests.cs
    File description: Related implementation.

    Created by: Senparc - 20260802
----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Tests;

[TestClass]
public class DesktopBridgeNeuBellProviderTests
{
    [TestMethod]
    public async Task GetSnapshotAsync_ShouldExposePendingPairingCount()
    {
        var store = new DesktopBridgeCredentialStore(legacyToken: null);
        var provider = new DesktopBridgeNeuBellProvider(store);

        var emptySnapshot = await provider.GetSnapshotAsync(new NeuBellRequestContext("admin"));
        Assert.AreEqual(Register.ModuleUid, provider.ModuleUid);
        Assert.AreEqual(0, emptySnapshot.Items.Single().Count);

        store.CreatePairingRequest("Desktop Test", "127.0.0.1");
        var pendingSnapshot = await provider.GetSnapshotAsync(new NeuBellRequestContext("admin"));

        Assert.AreEqual(DesktopBridgeNeuBellProvider.ProviderIdValue, pendingSnapshot.ProviderId);
        Assert.AreEqual(1, pendingSnapshot.Items.Single().Count);
        Assert.AreEqual("warning", pendingSnapshot.Items.Single().Severity);
    }
}
