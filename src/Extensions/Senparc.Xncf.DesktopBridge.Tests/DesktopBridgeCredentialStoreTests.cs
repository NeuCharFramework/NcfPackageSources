using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Tests;

[TestClass]
public sealed class DesktopBridgeCredentialStoreTests
{
    [TestMethod]
    public void ApproveAndPoll_IssuesIndependentRevocableSession()
    {
        var store = new DesktopBridgeCredentialStore(null);
        var pairing = store.CreatePairingRequest("测试工作台", "127.0.0.1");

        Assert.IsFalse(store.IsConfigured);
        Assert.IsTrue(store.Approve(pairing.RequestId, "admin"));

        var poll = store.Poll(pairing.RequestId, pairing.PollSecret);
        Assert.AreEqual("approved", poll.Status);
        Assert.IsFalse(string.IsNullOrWhiteSpace(poll.SessionToken));
        Assert.IsTrue(store.IsConfigured);
        Assert.IsTrue(store.IsAuthorized(poll.SessionToken));
        Assert.IsTrue(store.TryAuthorize(poll.SessionToken, out var sessionRevoked));
        Assert.IsFalse(sessionRevoked.IsCancellationRequested);

        var session = store.GetSessions().Single();
        Assert.AreEqual("测试工作台", session.ClientName);
        Assert.AreEqual("admin", session.ApprovedBy);
        Assert.IsNotNull(session.LastUsedAt);
        Assert.IsTrue(store.Revoke(session.SessionId));
        Assert.IsTrue(sessionRevoked.IsCancellationRequested);
        Assert.IsFalse(store.IsAuthorized(poll.SessionToken));
    }

    [TestMethod]
    public void Deny_PreventsSessionDelivery()
    {
        var store = new DesktopBridgeCredentialStore(null);
        var pairing = store.CreatePairingRequest("测试工作台", "127.0.0.1");

        Assert.IsTrue(store.Deny(pairing.RequestId));

        var poll = store.Poll(pairing.RequestId, pairing.PollSecret);
        Assert.AreEqual("denied", poll.Status);
        Assert.IsNull(poll.SessionToken);
        Assert.IsFalse(store.IsConfigured);
    }

    [TestMethod]
    public void LegacyEnvironmentToken_RemainsCompatible()
    {
        var store = new DesktopBridgeCredentialStore("legacy-token");

        Assert.IsTrue(store.IsConfigured);
        Assert.IsTrue(store.IsAuthorized("legacy-token"));
        Assert.IsFalse(store.IsAuthorized("wrong-token"));
    }

    [TestMethod]
    public void Poll_WithWrongSecret_DoesNotRevealPairingState()
    {
        var store = new DesktopBridgeCredentialStore(null);
        var pairing = store.CreatePairingRequest("测试工作台", "127.0.0.1");

        var poll = store.Poll(pairing.RequestId, "wrong-secret");

        Assert.AreEqual("invalid", poll.Status);
        Assert.IsFalse(store.Approve(Guid.NewGuid(), "admin"));
    }
}
