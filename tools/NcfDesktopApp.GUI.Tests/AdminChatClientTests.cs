using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NcfDesktopApp.GUI.Models;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.Tests;

[TestClass]
public sealed class AdminChatClientTests
{
    private const string SiteUrl = "http://localhost:5123";

    [TestMethod]
    public async Task AuthenticateAsync_UsesLoginThenAdminOnlyApi_AndKeepsOnlyToken()
    {
        var requests = new List<(string Path, string? Authorization, string Body)>();
        var requestIndex = 0;
        var client = CreateClient(request =>
        {
            requests.Add((
                request.RequestUri?.PathAndQuery ?? string.Empty,
                request.Headers.Authorization?.ToString(),
                request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty));

            return Interlocked.Increment(ref requestIndex) == 1
                ? JsonResponse("""
                    {
                      "success": true,
                      "data": {
                        "userName": "admin",
                        "token": "jwt-in-memory",
                        "tokenExpiresUtc": "2099-01-01T00:00:00Z"
                      }
                    }
                    """)
                : JsonResponse("""
                    {
                      "success": true,
                      "data": { "sessions": [], "totalCount": 0 }
                    }
                    """);
        });

        var result = await client.AuthenticateAsync(SiteUrl, "admin", "secret");

        Assert.AreEqual("admin", result.UserName);
        Assert.AreEqual("jwt-in-memory", client.Authentication?.AccessToken);
        Assert.IsTrue(client.IsAuthenticated);
        Assert.AreEqual(2, requests.Count);
        StringAssert.Contains(requests[0].Path, "AdminUserInfoAppService.LoginAsync");
        StringAssert.Contains(requests[0].Body, "secret");
        StringAssert.Contains(requests[1].Path, "AdminChatAppService.GetSessionListAsync");
        Assert.AreEqual("Bearer jwt-in-memory", requests[1].Authorization);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenAdminOnlyCheckIsForbidden_ClearsAuthentication()
    {
        var requestIndex = 0;
        var client = CreateClient(_ => Interlocked.Increment(ref requestIndex) == 1
            ? JsonResponse("""
                {
                  "success": true,
                  "data": {
                    "userName": "limited-user",
                    "token": "limited-token",
                    "tokenExpiresUtc": "2099-01-01T00:00:00Z"
                  }
                }
                """)
            : new HttpResponseMessage(HttpStatusCode.Forbidden));

        var exception = await Assert.ThrowsExceptionAsync<AdminChatApiException>(
            () => client.AuthenticateAsync(SiteUrl, "limited-user", "secret"));

        Assert.IsTrue(exception.IsAuthenticationFailure);
        Assert.IsNull(client.Authentication);
        Assert.IsFalse(client.IsAuthenticated);
    }

    [TestMethod]
    public async Task AuthenticateAsync_WhenSiteIsNotLoopback_DoesNotSendCredentials()
    {
        var requestSent = false;
        var client = CreateClient(_ =>
        {
            requestSent = true;
            return JsonResponse("{}");
        });

        await Assert.ThrowsExceptionAsync<AdminChatApiException>(
            () => client.AuthenticateAsync("https://example.com", "admin", "secret"));

        Assert.IsFalse(requestSent);
    }

    private static AdminChatClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        return new AdminChatClient(new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            Timeout = Timeout.InfiniteTimeSpan
        });
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
