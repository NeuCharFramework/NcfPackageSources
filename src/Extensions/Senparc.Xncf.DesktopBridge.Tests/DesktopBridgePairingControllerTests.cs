using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.DesktopBridge.Models;
using Senparc.Xncf.DesktopBridge.OHS.Local.Controllers;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Tests;

[TestClass]
public sealed class DesktopBridgePairingControllerTests
{
    [TestMethod]
    public void CreateRequest_AllowsLoopbackHttp()
    {
        var controller = CreateController(IPAddress.Loopback, isHttps: false);

        var result = controller.CreateRequest(new DesktopBridgePairingCreateRequest("测试工作台"));

        var ok = result.Result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.IsInstanceOfType<DesktopBridgePairingCreateResponse>(ok.Value);
    }

    [TestMethod]
    public void CreateRequest_RejectsRemoteHttp()
    {
        var controller = CreateController(IPAddress.Parse("203.0.113.10"), isHttps: false);

        var result = controller.CreateRequest(new DesktopBridgePairingCreateRequest("测试工作台"));

        var forbidden = result.Result as ObjectResult;
        Assert.IsNotNull(forbidden);
        Assert.AreEqual(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [TestMethod]
    public async Task GetEvents_WhenSessionIsRevoked_EndsExistingStream()
    {
        var store = new DesktopBridgeCredentialStore(null);
        var pairing = store.CreatePairingRequest("测试工作台", "127.0.0.1");
        Assert.IsTrue(store.Approve(pairing.RequestId, "admin"));
        var poll = store.Poll(pairing.RequestId, pairing.PollSecret);
        var session = store.GetSessions().Single();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[DesktopBridgeTokenValidator.TokenHeaderName] = poll.SessionToken;
        httpContext.Response.Body = new MemoryStream();
        var controller = new DesktopBridgeController(
            new DesktopActivityHub(),
            new DesktopAuthorizedSyncHub(),
            new DesktopBridgeTokenValidator(store))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var streamTask = controller.GetEvents(replayBuffered: false);
        await Task.Delay(50);
        Assert.IsFalse(streamTask.IsCompleted);

        Assert.IsTrue(store.Revoke(session.SessionId));
        await streamTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsTrue(streamTask.IsCompletedSuccessfully);
    }

    private static DesktopBridgePairingController CreateController(IPAddress remoteAddress, bool isHttps)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteAddress;
        httpContext.Request.Scheme = isHttps ? "https" : "http";
        return new DesktopBridgePairingController(new DesktopBridgeCredentialStore(null))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
