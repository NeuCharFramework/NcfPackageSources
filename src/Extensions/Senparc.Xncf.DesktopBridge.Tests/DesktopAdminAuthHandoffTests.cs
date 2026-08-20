using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Shared.Abstractions.Security;
using Senparc.Xncf.DesktopBridge.Models;
using Senparc.Xncf.DesktopBridge.OHS.Local.Controllers;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Tests;

[TestClass]
public sealed class DesktopAdminAuthHandoffTests
{
    [TestMethod]
    public void Store_RequiresDesktopBindingAndPkce_AndAllowsOnlyOneRedemption()
    {
        var store = new DesktopAdminAuthHandoffStore();
        var verifier = CreateVerifier();
        var challenge = CreateChallenge(verifier);
        var created = store.Create("desktop-session", challenge, "/Admin/Index");

        Assert.AreEqual("invalid", store.Redeem(created.RequestId, "other-session", verifier).Status);
        Assert.AreEqual("invalid", store.Redeem(created.RequestId, "desktop-session", CreateVerifier()).Status);
        Assert.AreEqual("pending", store.Redeem(created.RequestId, "desktop-session", verifier).Status);

        var cookieExpiry = DateTimeOffset.UtcNow.AddMinutes(20);
        Assert.IsTrue(store.Approve(created.RequestId, 42, "admin", cookieExpiry));
        var approved = store.Redeem(created.RequestId, "desktop-session", verifier);

        Assert.AreEqual("approved", approved.Status);
        Assert.AreEqual(42, approved.AdminUserId);
        Assert.AreEqual("admin", approved.UserName);
        Assert.AreEqual(cookieExpiry, approved.SourceAuthenticationExpiresUtc);
        Assert.AreEqual("invalid", store.Redeem(created.RequestId, "desktop-session", verifier).Status);
    }

    [TestMethod]
    public void Store_LimitsOutstandingChallengesPerDesktopSession()
    {
        var store = new DesktopAdminAuthHandoffStore();
        for (var i = 0; i < 3; i++)
        {
            var verifier = CreateVerifier();
            _ = store.Create("desktop-session", CreateChallenge(verifier), "/Admin/Index");
        }

        var finalVerifier = CreateVerifier();
        Assert.ThrowsException<DesktopAdminAuthHandoffRateLimitException>(() =>
            store.Create("desktop-session", CreateChallenge(finalVerifier), "/Admin/Index"));
    }

    [TestMethod]
    public void Store_ReplacesNonLocalReturnPath()
    {
        var store = new DesktopAdminAuthHandoffStore();
        var verifier = CreateVerifier();

        var created = store.Create(
            "desktop-session",
            CreateChallenge(verifier),
            "https://attacker.example/steal");

        StringAssert.Contains(created.ApprovalPath, "returnUrl=%2FAdmin%2FIndex");
        Assert.IsFalse(created.ApprovalPath.Contains("attacker", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Controller_IssuesTokenOnlyAfterCookieApprovalAndConsumesChallenge()
    {
        var credentialStore = new DesktopBridgeCredentialStore("desktop-session");
        var handoffStore = new DesktopAdminAuthHandoffStore();
        var issuer = new StubTokenIssuer();
        var controller = CreateController(credentialStore, handoffStore, issuer);
        var verifier = CreateVerifier();

        var createResult = controller.CreateAdminAuthHandoff(
            new DesktopAdminAuthHandoffCreateRequest(CreateChallenge(verifier), "/Admin/Index"));
        var created = (createResult.Result as OkObjectResult)?.Value as DesktopAdminAuthHandoffCreateResponse;
        Assert.IsNotNull(created);
        Assert.IsTrue(handoffStore.Approve(
            created.RequestId,
            42,
            "admin",
            DateTimeOffset.UtcNow.AddMinutes(20)));

        var redeemResult = await controller.RedeemAdminAuthHandoff(
            new DesktopAdminAuthHandoffRedeemRequest(created.RequestId, verifier));
        var redeemed = (redeemResult.Result as OkObjectResult)?.Value as DesktopAdminAuthHandoffRedeemResponse;

        Assert.IsNotNull(redeemed);
        Assert.AreEqual("approved", redeemed.Status);
        Assert.AreEqual("issued-jwt", redeemed.AccessToken);
        Assert.AreEqual(42, issuer.LastAdminUserId);

        var replayResult = await controller.RedeemAdminAuthHandoff(
            new DesktopAdminAuthHandoffRedeemRequest(created.RequestId, verifier));
        var replay = (replayResult.Result as BadRequestObjectResult)?.Value as DesktopAdminAuthHandoffRedeemResponse;
        Assert.AreEqual("invalid", replay?.Status);
    }

    [TestMethod]
    public void Controller_RejectsRemoteHttpAndMissingDesktopSession()
    {
        var credentialStore = new DesktopBridgeCredentialStore("desktop-session");
        var verifier = CreateVerifier();
        var request = new DesktopAdminAuthHandoffCreateRequest(CreateChallenge(verifier), "/Admin/Index");
        var remoteHttp = CreateController(
            credentialStore,
            new DesktopAdminAuthHandoffStore(),
            new StubTokenIssuer(),
            IPAddress.Parse("203.0.113.10"));
        var missingToken = CreateController(
            credentialStore,
            new DesktopAdminAuthHandoffStore(),
            new StubTokenIssuer(),
            IPAddress.Loopback,
            includeToken: false);

        var remoteResult = remoteHttp.CreateAdminAuthHandoff(request).Result as ObjectResult;
        var missingTokenResult = missingToken.CreateAdminAuthHandoff(request).Result;

        Assert.AreEqual(StatusCodes.Status403Forbidden, remoteResult?.StatusCode);
        Assert.IsInstanceOfType<UnauthorizedResult>(missingTokenResult);
    }

    [TestMethod]
    public void Capabilities_AdvertiseHandoffOnlyWhenAdminIssuerExists()
    {
        var credentialStore = new DesktopBridgeCredentialStore("desktop-session");
        var withoutIssuer = CreateController(credentialStore, new DesktopAdminAuthHandoffStore(), null);
        var withIssuer = CreateController(credentialStore, new DesktopAdminAuthHandoffStore(), new StubTokenIssuer());

        Assert.IsFalse(withoutIssuer.GetCapabilities().Value?.SupportsAdminAuthHandoff);
        Assert.IsTrue(withIssuer.GetCapabilities().Value?.SupportsAdminAuthHandoff);
    }

    private static DesktopBridgeController CreateController(
        DesktopBridgeCredentialStore credentialStore,
        DesktopAdminAuthHandoffStore handoffStore,
        IDesktopAdminAuthTokenIssuer? issuer,
        IPAddress? remoteAddress = null,
        bool includeToken = true)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteAddress ?? IPAddress.Loopback;
        httpContext.Request.Scheme = "http";
        if (includeToken)
        {
            httpContext.Request.Headers[DesktopBridgeTokenValidator.TokenHeaderName] = "desktop-session";
        }
        return new DesktopBridgeController(
            new DesktopActivityHub(),
            new DesktopAuthorizedSyncHub(),
            new DesktopBridgeTokenValidator(credentialStore),
            handoffStore,
            issuer == null ? [] : [issuer])
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static string CreateVerifier() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string CreateChallenge(string verifier) =>
        WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private sealed class StubTokenIssuer : IDesktopAdminAuthTokenIssuer
    {
        public int? LastAdminUserId { get; private set; }

        public Task<DesktopAdminAuthTokenIssueResult> IssueAsync(
            int adminUserId,
            DateTimeOffset sourceAuthenticationExpiresUtc,
            CancellationToken cancellationToken = default)
        {
            LastAdminUserId = adminUserId;
            return Task.FromResult(new DesktopAdminAuthTokenIssueResult(
                true,
                "admin",
                "issued-jwt",
                sourceAuthenticationExpiresUtc));
        }
    }
}
