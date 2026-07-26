using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NcfDesktopApp.GUI.Models;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.Tests;

[TestClass]
public sealed class DesktopBridgeClientTests
{
    private const string SiteUrl = "http://localhost:5123";
    private const string Token = "test-session-token";

    [TestMethod]
    public async Task ProbeAsync_WhenEndpointIsMissing_ReturnsNotInstalledWithoutThrowing()
    {
        await using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await client.ProbeAsync(SiteUrl, Token);

        Assert.AreEqual(DesktopBridgeAvailability.NotInstalled, result.Availability);
        Assert.IsFalse(result.IsAvailable);
    }

    [TestMethod]
    public async Task ProbeAsync_WhenBridgeIsInactive_ReturnsInactiveWithoutThrowing()
    {
        await using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await client.ProbeAsync(SiteUrl, Token);

        Assert.AreEqual(DesktopBridgeAvailability.Inactive, result.Availability);
    }

    [TestMethod]
    public async Task ProbeAsync_WhenPayloadIsInvalid_ReturnsIncompatibleWithoutThrowing()
    {
        await using var client = CreateClient(_ => JsonResponse("not-json"));

        var result = await client.ProbeAsync(SiteUrl, Token);

        Assert.AreEqual(DesktopBridgeAvailability.Incompatible, result.Availability);
    }

    [TestMethod]
    public async Task ProbeAsync_WhenTransportFails_ReturnsUnavailableWithoutThrowing()
    {
        await using var client = CreateClient(_ => throw new HttpRequestException("connection refused"));

        var result = await client.ProbeAsync(SiteUrl, Token);

        Assert.AreEqual(DesktopBridgeAvailability.Unavailable, result.Availability);
    }

    [TestMethod]
    public async Task ProbeAsync_WhenProtocolMatches_ReturnsAvailableAndSendsToken()
    {
        string? receivedToken = null;
        await using var client = CreateClient(request =>
        {
            receivedToken = request.Headers.GetValues(DesktopBridgeClient.TokenHeaderName).Single();
            return JsonResponse("""
                {
                  "protocolVersion": 1,
                  "bridgeVersion": "0.1.0-preview1",
                  "supportsSse": true,
                  "supportsSnapshot": true,
                  "eventEndpoint": "/api/Senparc.Xncf.DesktopBridge/events",
                  "snapshotEndpoint": "/api/Senparc.Xncf.DesktopBridge/activities"
                }
                """);
        });

        var result = await client.ProbeAsync(SiteUrl, Token);

        Assert.AreEqual(DesktopBridgeAvailability.Available, result.Availability);
        Assert.AreEqual(Token, receivedToken);
        Assert.AreEqual(DesktopBridgeClient.SupportedProtocolVersion, result.Capabilities?.ProtocolVersion);
    }

    [TestMethod]
    public async Task ProbeAsync_WhenAddressIsNotLoopback_ReturnsUnavailableWithoutSendingRequest()
    {
        var requestSent = false;
        await using var client = CreateClient(_ =>
        {
            requestSent = true;
            return JsonResponse("{}");
        });

        var result = await client.ProbeAsync("https://example.com", Token);

        Assert.AreEqual(DesktopBridgeAvailability.Unavailable, result.Availability);
        Assert.IsFalse(requestSent);
    }

    [TestMethod]
    public async Task ConnectAsync_WhenInitialTransportFails_RetriesAndRecoversWithoutThrowing()
    {
        var attempt = 0;
        await using var client = CreateClient(_ =>
        {
            if (Interlocked.Increment(ref attempt) == 1)
            {
                throw new HttpRequestException("connection refused");
            }

            return JsonResponse("""
                {
                  "protocolVersion": 1,
                  "bridgeVersion": "0.1.0-preview1",
                  "supportsSse": true,
                  "supportsSnapshot": true,
                  "eventEndpoint": "/api/Senparc.Xncf.DesktopBridge/events",
                  "snapshotEndpoint": "/api/Senparc.Xncf.DesktopBridge/activities"
                }
                """);
        });
        var recovered = new TaskCompletionSource<DesktopBridgeProbeResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        client.AvailabilityChanged += result =>
        {
            if (result.IsAvailable)
            {
                recovered.TrySetResult(result);
            }
        };

        var initialResult = await client.ConnectAsync(SiteUrl, Token);
        var recoveredResult = await recovered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(DesktopBridgeAvailability.Unavailable, initialResult.Availability);
        Assert.AreEqual(DesktopBridgeAvailability.Available, recoveredResult.Availability);
        Assert.IsTrue(attempt >= 2);
    }

    [TestMethod]
    public async Task StartAuthorizedSyncAsync_SendsDesktopAndBearerTokens_AndReadsNotification()
    {
        string? desktopToken = null;
        string? authorization = null;
        await using var client = CreateClient(request =>
        {
            desktopToken = request.Headers.GetValues(DesktopBridgeClient.TokenHeaderName).Single();
            authorization = request.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    event: authorized-sync
                    id: 1
                    data: {"sequence":1,"channel":"admin-chat","resourceId":"42","action":"messages-changed","time":"2026-07-26T00:00:00Z"}


                    """, Encoding.UTF8, "text/event-stream")
            };
        });
        var received = new TaskCompletionSource<DesktopAuthorizedSyncMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        client.AuthorizedSyncReceived += message => received.TrySetResult(message);

        await client.StartAuthorizedSyncAsync(
            SiteUrl,
            Token,
            "admin-jwt",
            "/api/Senparc.Xncf.DesktopBridge/authorized-sync/events");
        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await client.StopAuthorizedSyncAsync();

        Assert.AreEqual(Token, desktopToken);
        Assert.AreEqual("Bearer admin-jwt", authorization);
        Assert.AreEqual("admin-chat", message.Channel);
        Assert.AreEqual("42", message.ResourceId);
    }

    private static DesktopBridgeClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        return new DesktopBridgeClient(httpClient);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
