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
