using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Tests.WeixinClaw;

[TestClass]
public class WeixinClawProtocolTests
{
    [TestMethod]
    public async Task GetQrCodeAsync_LiveEndpoint_ReturnsQrWhenExplicitlyEnabled()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("WEIXINCLAW_LIVE_TEST"),
                "1",
                StringComparison.Ordinal))
        {
            Assert.Inconclusive("Set WEIXINCLAW_LIVE_TEST=1 to run the live iLink test.");
        }

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        var api = new WeixinClawApi(httpClient);

        var response = await api.GetQrCodeAsync();

        Assert.IsFalse(string.IsNullOrWhiteSpace(response.Qrcode));
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.QrcodeImageContent));
    }

    [TestMethod]
    public async Task GetQrCodeAsync_LiveEndpoint_TransportVariants()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("WEIXINCLAW_LIVE_TEST"),
                "1",
                StringComparison.Ordinal))
        {
            Assert.Inconclusive("Set WEIXINCLAW_LIVE_TEST=1 to run the live iLink test.");
        }

        using var defaultClient = CreateLiveClient();
        using var http11Client = CreateLiveClient();
        http11Client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
        http11Client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        using var browserLikeClient = CreateLiveClient();
        browserLikeClient.DefaultRequestVersion = System.Net.HttpVersion.Version11;
        browserLikeClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        browserLikeClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenClaw");
        browserLikeClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        browserLikeClient.DefaultRequestHeaders.ExpectContinue = false;

        var results = new[]
        {
            await TryLiveQrAsync("default", defaultClient),
            await TryLiveQrAsync("http11", http11Client),
            await TryLiveQrAsync("browser-like", browserLikeClient)
        };

        foreach (var result in results)
        {
            Console.WriteLine(result);
        }

        Assert.IsTrue(results.Any(z => z.Contains(":ok:", StringComparison.Ordinal)), string.Join(Environment.NewLine, results));
    }

    [TestMethod]
    public async Task GetQrCodeAsync_IncludesPostProtocolHeadersWithoutBearerToken()
    {
        var handler = new CapturingHandler(
            """{"qrcode":"qr","qrcode_img_content":"https://example.test/qr","ret":0}""");
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var response = await api.GetQrCodeAsync();

        Assert.AreEqual("qr", response.Qrcode);
        Assert.AreEqual("ilink_bot_token", handler.Request.Headers.GetValues("AuthorizationType").Single());
        Assert.AreEqual("bot", handler.Request.Headers.GetValues("iLink-App-Id").Single());
        Assert.AreEqual("132105", handler.Request.Headers.GetValues("iLink-App-ClientVersion").Single());
        Assert.IsTrue(handler.Request.Headers.Contains("X-WECHAT-UIN"));
        Assert.IsFalse(handler.Request.Headers.Contains("Authorization"));
        Assert.AreEqual("application/json", handler.Request.Content.Headers.ContentType?.ToString());
        StringAssert.Contains(handler.Body, "\"local_token_list\":[]");
        Assert.IsFalse(handler.Body.Contains("base_info", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task GetUpdatesAsync_BuildsIlinkHeadersAndPreservesUint64MessageId()
    {
        var handler = new CapturingHandler(
            """
            {
              "ret": 0,
              "msgs": [
                {
                  "seq": 7,
                  "message_id": 18446744073709551615,
                  "message_type": 1,
                  "item_list": [
                    { "type": 1, "text_item": { "text": "hello" } }
                  ]
                }
              ],
              "get_updates_buf": "next-cursor"
            }
            """);
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var response = await api.GetUpdatesAsync(
            "https://ilink.example.test",
            "bot-secret",
            "old-cursor");

        Assert.AreEqual("next-cursor", response.GetUpdatesBuf);
        Assert.AreEqual("18446744073709551615", response.Msgs[0].MessageId);
        Assert.AreEqual("hello", response.Msgs[0].ItemList[0].TextItem.Text);
        Assert.AreEqual("Bearer bot-secret", handler.Request.Headers.Authorization.ToString());
        Assert.AreEqual("ilink_bot_token", handler.Request.Headers.GetValues("AuthorizationType").Single());
        Assert.AreEqual("bot", handler.Request.Headers.GetValues("iLink-App-Id").Single());
        Assert.AreEqual("132105", handler.Request.Headers.GetValues("iLink-App-ClientVersion").Single());
        Assert.IsTrue(handler.Request.Headers.Contains("X-WECHAT-UIN"));
        StringAssert.Contains(handler.Body, "\"get_updates_buf\":\"old-cursor\"");
        StringAssert.Contains(handler.Request.RequestUri.AbsolutePath, "/ilink/bot/getupdates");
    }

    [TestMethod]
    public async Task GetQrCodeStatusAsync_UsesUnauthenticatedLongPollRequest()
    {
        var handler = new CapturingHandler("""{"status":"wait"}""");
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var response = await api.GetQrCodeStatusAsync("qr value", cancellationToken: CancellationToken.None);

        Assert.AreEqual("wait", response.Status);
        Assert.IsFalse(handler.Request.Headers.Contains("Authorization"));
        Assert.IsTrue(handler.Request.RequestUri.Query.Contains("qrcode=qr%20value"));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _response;

        public CapturingHandler(string response)
        {
            _response = response;
        }

        public HttpRequestMessage Request { get; private set; }
        public string Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json")
            };
        }
    }

    private static HttpClient CreateLiveClient() => new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private static async Task<string> TryLiveQrAsync(string name, HttpClient client)
    {
        try
        {
            var response = await new WeixinClawApi(client).GetQrCodeAsync();
            return $"{name}:ok:{response.Qrcode}";
        }
        catch (Exception ex)
        {
            return $"{name}:error:{ex.Message}";
        }
    }
}
