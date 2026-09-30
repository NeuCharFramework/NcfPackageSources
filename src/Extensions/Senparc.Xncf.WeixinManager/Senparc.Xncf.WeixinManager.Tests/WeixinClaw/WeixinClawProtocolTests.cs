using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
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
    public void WeixinClawQrCodeRenderer_ReturnsPngDataUri()
    {
        var dataUri = WeixinClawQrCodeRenderer.RenderDataUri(
            "https://liteapp.weixin.qq.com/q/7GiQu1?qrcode=test&bot_type=3");

        StringAssert.StartsWith(dataUri, "data:image/png;base64,");
        var png = Convert.FromBase64String(dataUri["data:image/png;base64,".Length..]);
        CollectionAssert.AreEqual(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            png[..8]);
        Assert.IsTrue(png.Length > 100);
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
        Assert.AreEqual("132104", handler.Request.Headers.GetValues("iLink-App-ClientVersion").Single());
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
                  "context_token": "ctx-7",
                  "run_id": "run-7",
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
        Assert.AreEqual("ctx-7", response.Msgs[0].ContextToken);
        Assert.AreEqual("run-7", response.Msgs[0].RunId);
        Assert.AreEqual("Bearer bot-secret", handler.Request.Headers.Authorization.ToString());
        Assert.AreEqual("ilink_bot_token", handler.Request.Headers.GetValues("AuthorizationType").Single());
        Assert.AreEqual("bot", handler.Request.Headers.GetValues("iLink-App-Id").Single());
        Assert.AreEqual("132104", handler.Request.Headers.GetValues("iLink-App-ClientVersion").Single());
        Assert.IsTrue(handler.Request.Headers.Contains("X-WECHAT-UIN"));
        StringAssert.Contains(handler.Body, "\"get_updates_buf\":\"old-cursor\"");
        StringAssert.Contains(handler.Body, "\"channel_version\":\"0.1.0\"");
        StringAssert.Contains(handler.Body, "\"bot_agent\":\"NCF-WeixinManager/0.1.0\"");
        StringAssert.Contains(handler.Request.RequestUri.AbsolutePath, "/ilink/bot/getupdates");
    }

    [TestMethod]
    public async Task SendMessageAsync_IncludesRecipientAndContextToken()
    {
        var handler = new CapturingHandler("""{"ret":0,"message_id":"out-1"}""");
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var response = await api.SendMessageAsync(
            "https://ilink.example.test",
            "bot-secret",
            new WeixinClawMessage
            {
                ToUserId = "user@im.wechat",
                ClientId = "client-1",
                MessageType = 2,
                MessageState = 2,
                ContextToken = "ctx-1",
                RunId = "run-1",
                ItemList =
                [
                    new WeixinClawMessageItem
                    {
                        Type = 1,
                        TextItem = new WeixinClawTextItem { Text = "hi" }
                    }
                ]
            });

        Assert.AreEqual(0, response.Ret);
        Assert.AreEqual("out-1", response.MessageId);
        StringAssert.Contains(handler.Body, "\"to_user_id\":\"user@im.wechat\"");
        StringAssert.Contains(handler.Body, "\"context_token\":\"ctx-1\"");
        StringAssert.Contains(handler.Body, "\"message_state\":2");
        StringAssert.Contains(handler.Body, "\"run_id\":\"run-1\"");
        StringAssert.Contains(handler.Request.RequestUri.AbsolutePath, "/ilink/bot/sendmessage");
    }

    [TestMethod]
    public async Task DownloadMediaAsync_DecryptsAesEcbPayload()
    {
        var key = Enumerable.Range(1, 16).Select(value => (byte)value).ToArray();
        var plain = Encoding.UTF8.GetBytes("media-content");
        byte[] encrypted;
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            using var encryptor = aes.CreateEncryptor();
            encrypted = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        }

        var handler = new CapturingHandler(encrypted);
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var result = await api.DownloadMediaAsync(new WeixinClawCdnMedia
        {
            EncryptQueryParam = "download-param",
            AesKey = Convert.ToBase64String(key),
            EncryptType = 1
        });

        CollectionAssert.AreEqual(plain, result);
        Assert.IsTrue(handler.Request.RequestUri.AbsolutePath.EndsWith("/c2c/download"));
        Assert.IsTrue(handler.Request.RequestUri.Query.Contains("encrypted_query_param=download-param"));
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

    [TestMethod]
    public async Task GetQrCodeStatusAsync_UsesProvidedRedirectBaseUrl()
    {
        var handler = new CapturingHandler("""{"status":"confirmed"}""");
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var response = await api.GetQrCodeStatusAsync(
            "redirected-qr",
            baseUrl: "https://redirect.example.test/bot-gateway/",
            cancellationToken: CancellationToken.None);

        Assert.AreEqual("confirmed", response.Status);
        Assert.AreEqual("redirect.example.test", handler.Request.RequestUri.Host);
        Assert.AreEqual("/bot-gateway/ilink/bot/get_qrcode_status", handler.Request.RequestUri.AbsolutePath);
        Assert.IsTrue(handler.Request.RequestUri.Query.Contains("qrcode=redirected-qr"));
    }

    [TestMethod]
    public async Task GetQrCodeStatusAsync_UsesHttpsRedirectHostBaseUrl()
    {
        var handler = new CapturingHandler("""{"status":"wait"}""");
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var response = await api.GetQrCodeStatusAsync(
            "redirect-host-qr",
            baseUrl: "https://redirect-host.example.test",
            cancellationToken: CancellationToken.None);

        Assert.AreEqual("wait", response.Status);
        Assert.AreEqual("redirect-host.example.test", handler.Request.RequestUri.Host);
        Assert.AreEqual("/ilink/bot/get_qrcode_status", handler.Request.RequestUri.AbsolutePath);
        Assert.IsTrue(handler.Request.RequestUri.Query.Contains("qrcode=redirect-host-qr"));
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly byte[] _binaryResponse;

        public CapturingHandler(string response)
        {
            _response = response;
        }

        public CapturingHandler(byte[] response)
        {
            _binaryResponse = response;
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
                Content = _binaryResponse == null
                    ? new StringContent(_response, Encoding.UTF8, "application/json")
                    : new ByteArrayContent(_binaryResponse)
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
