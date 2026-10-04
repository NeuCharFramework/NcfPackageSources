using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
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
        Assert.AreEqual("132105", handler.Request.Headers.GetValues("iLink-App-ClientVersion").Single());
        Assert.IsTrue(handler.Request.Headers.Contains("X-WECHAT-UIN"));
        Assert.IsFalse(handler.Request.Headers.Contains("Authorization"));
        Assert.AreEqual("application/json", handler.Request.Content.Headers.ContentType?.ToString());
        AssertJsonBody("""{"local_token_list":[]}""", handler.Body);
        Assert.AreEqual(HttpMethod.Post, handler.Request.Method);
        Assert.AreEqual("/ilink/bot/get_bot_qrcode", handler.Request.RequestUri.AbsolutePath);
        Assert.AreEqual("?bot_type=3", handler.Request.RequestUri.Query);
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
        Assert.AreEqual("132105", handler.Request.Headers.GetValues("iLink-App-ClientVersion").Single());
        Assert.IsTrue(handler.Request.Headers.Contains("X-WECHAT-UIN"));
        AssertJsonBody(
            """
            {"get_updates_buf":"old-cursor",
             "base_info":{"channel_version":"2.4.9","bot_agent":"NCF-WeixinManager/2.4.9"}}
            """,
            handler.Body);
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
        AssertJsonBody(
            """
            {
              "msg":{
                "from_user_id":"","to_user_id":"user@im.wechat","client_id":"client-1",
                "message_type":2,"message_state":2,"context_token":"ctx-1","run_id":"run-1",
                "item_list":[{"type":1,"text_item":{"text":"hi"}}]
              },
              "base_info":{"channel_version":"2.4.9","bot_agent":"NCF-WeixinManager/2.4.9"}
            }
            """,
            handler.Body);
        Assert.AreEqual("/ilink/bot/sendmessage", handler.Request.RequestUri.AbsolutePath);
        AssertPostHeaders(handler.Request);
    }

    [TestMethod]
    public async Task SendMessageAsync_OmitsInboundFieldsAndNullItemsWithoutChangingTextOrContext()
    {
        var handler = new CapturingHandler("""{"ret":0}""");
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var response = await api.SendMessageAsync(
            "https://ilink.example.test",
            "bot-secret",
            new WeixinClawMessage
            {
                Seq = 99,
                MessageId = "inbound-1",
                FromUserId = "inbound-user",
                CreateTimeMs = 12345,
                SessionId = "server-session",
                GroupId = "server-group",
                ToUserId = "user@im.wechat",
                ClientId = "client-1",
                MessageType = 2,
                MessageState = 2,
                ContextToken = " ctx+/= ",
                ItemList = [new() { Type = 1, TextItem = new() { Text = "你好 \"微信\"\nClaw 👋" } }]
            });

        Assert.AreEqual(0, response.Ret);
        Assert.IsNull(response.MessageId);
        AssertJsonBody(
            """
            {
              "msg":{
                "from_user_id":"","to_user_id":"user@im.wechat","client_id":"client-1",
                "message_type":2,"message_state":2,"context_token":" ctx+/= ",
                "item_list":[{"type":1,"text_item":{"text":"你好 \"微信\"\nClaw 👋"}}]
              },
              "base_info":{"channel_version":"2.4.9","bot_agent":"NCF-WeixinManager/2.4.9"}
            }
            """,
            handler.Body);
    }

    [DataTestMethod]
    [DataRow("""{"ret":-14,"errmsg":"session expired"}""", "-14")]
    [DataRow("""{"ret":1,"errmsg":"invalid message"}""", "invalid message")]
    public async Task SendMessageAsync_RejectsBusinessErrors(string responseJson, string error)
    {
        var handler = new CapturingHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);

        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            api.SendMessageAsync("https://ilink.example.test", "bot-secret", new WeixinClawMessage()));

        StringAssert.Contains(exception.Message, error);
    }

    [TestMethod]
    public async Task SendMessageAsync_PreservesUint64ServerMessageId()
    {
        var handler = new CapturingHandler("""{"ret":0,"message_id":18446744073709551615}""");
        using var httpClient = new HttpClient(handler);
        var response = await new WeixinClawApi(httpClient).SendMessageAsync(
            "https://ilink.example.test", "bot-secret", new WeixinClawMessage());

        Assert.AreEqual("18446744073709551615", response.MessageId);
    }

    [DataTestMethod]
    [DataRow("{}")]
    [DataRow("""{"message_id":"out-1"}""")]
    public async Task SendMessageAsync_AllowsOfficialOptionalRetWithoutInventingZero(string responseJson)
    {
        var handler = new CapturingHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var response = await new WeixinClawApi(httpClient).SendMessageAsync(
            "https://ilink.example.test", "bot-secret", new WeixinClawMessage());

        Assert.IsNull(response.Ret);
    }

    [DataTestMethod]
    [DataRow(2, """{"type":2,"image_item":{"media":{"encrypt_query_param":"download-param","aes_key":"key-base64","encrypt_type":1},"mid_size":32}}""")]
    [DataRow(3, """{"type":3,"voice_item":{"media":{"encrypt_query_param":"download-param","aes_key":"key-base64","encrypt_type":1},"encode_type":6}}""")]
    [DataRow(4, """{"type":4,"file_item":{"media":{"encrypt_query_param":"download-param","aes_key":"key-base64","encrypt_type":1},"file_name":"test.txt","len":"17"}}""")]
    public async Task SendMessageAsync_MediaItemsMatchOfficialFieldNamesAndTypes(int type, string expectedItem)
    {
        var media = new WeixinClawCdnMedia
        {
            EncryptQueryParam = "download-param", AesKey = "key-base64", EncryptType = 1
        };
        var item = new WeixinClawMessageItem { Type = type };
        switch (type)
        {
            case 2:
                item.ImageItem = new() { Media = media, MidSize = 32 };
                break;
            case 3:
                item.VoiceItem = new() { Media = media, EncodeType = 6 };
                break;
            case 4:
                item.FileItem = new() { Media = media, FileName = "test.txt", Len = "17" };
                break;
        }
        var handler = new CapturingHandler("""{"ret":0}""");
        using var httpClient = new HttpClient(handler);
        await new WeixinClawApi(httpClient).SendMessageAsync(
            "https://ilink.example.test", "bot-secret",
            new WeixinClawMessage
            {
                ToUserId = "user@im.wechat", ClientId = "client-1", MessageType = 2,
                MessageState = 2, ContextToken = "ctx-1", ItemList = [item]
            });

        AssertJsonBody(
            $$"""
            {
              "msg":{
                "from_user_id":"","to_user_id":"user@im.wechat","client_id":"client-1",
                "message_type":2,"message_state":2,"context_token":"ctx-1",
                "item_list":[{{expectedItem}}]
              },
              "base_info":{"channel_version":"2.4.9","bot_agent":"NCF-WeixinManager/2.4.9"}
            }
            """,
            handler.Body);
    }

    [TestMethod]
    public async Task GetUploadUrlAsync_MatchesOfficialRequestContract()
    {
        var handler = new CapturingHandler("""{"upload_param":"upload","upload_full_url":"https://cdn.example.test/upload"}""");
        using var httpClient = new HttpClient(handler);
        var response = await new WeixinClawApi(httpClient).GetUploadUrlAsync(
            "https://ilink.example.test", "bot-secret",
            new WeixinClawGetUploadUrlRequest
            {
                FileKey = "file-key", MediaType = 3, ToUserId = "user@im.wechat",
                RawSize = 17, RawFileMd5 = "plain-md5", FileSize = 32,
                NoNeedThumb = true, AesKey = "000102030405060708090a0b0c0d0e0f"
            });

        Assert.AreEqual("upload", response.UploadParam);
        AssertJsonBody(
            """
            {"filekey":"file-key","media_type":3,"to_user_id":"user@im.wechat",
             "rawsize":17,"rawfilemd5":"plain-md5","filesize":32,"no_need_thumb":true,
             "aeskey":"000102030405060708090a0b0c0d0e0f",
             "base_info":{"channel_version":"2.4.9","bot_agent":"NCF-WeixinManager/2.4.9"}}
            """,
            handler.Body);
        Assert.AreEqual("/ilink/bot/getuploadurl", handler.Request.RequestUri.AbsolutePath);
        AssertPostHeaders(handler.Request);
    }

    [TestMethod]
    public async Task GetUpdatesAsync_PreservesOfficialVideoMetadataAndCdnReference()
    {
        var handler = new CapturingHandler(
            """
            {"ret":0,"msgs":[{"message_type":1,"item_list":[{"type":5,"video_item":{
              "video_size":1024,"play_length":1000,"media":{
                "full_url":"https://cdn.example.test/video","encrypt_query_param":"param",
                "aes_key":"key","encrypt_type":1
              }
            }}]}]}
            """);
        using var client = new HttpClient(handler);
        var response = await new WeixinClawApi(client).GetUpdatesAsync(
            "https://ilink.example.test", "bot-secret", "");

        var video = response.Msgs.Single().ItemList.Single().VideoItem;
        Assert.AreEqual(1024L, video.VideoSize);
        Assert.AreEqual(1000, video.PlayLength);
        Assert.AreEqual("https://cdn.example.test/video", video.Media.FullUrl);
        Assert.AreEqual("param", video.Media.EncryptQueryParam);
    }

    [TestMethod]
    public async Task DownloadMediaAsync_UsesOfficialFullUrlWithoutBotCredentials()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var handler = new CapturingHandler(bytes);
        using var client = new HttpClient(handler);
        var result = await new WeixinClawApi(client).DownloadMediaAsync(new WeixinClawCdnMedia
        {
            FullUrl = "https://cdn.example.test/video?param=signed", EncryptType = 0
        });

        CollectionAssert.AreEqual(bytes, result);
        Assert.AreEqual("https://cdn.example.test/video?param=signed", handler.Request.RequestUri.AbsoluteUri);
        Assert.IsNull(handler.Request.Headers.Authorization);
    }

    [DataTestMethod]
    [DataRow("getconfig")]
    [DataRow("sendtyping")]
    [DataRow("notifystart")]
    [DataRow("notifystop")]
    public async Task AuxiliaryRequests_MatchOfficialRequestContracts(string operation)
    {
        var handler = new CapturingHandler("""{"ret":0,"typing_ticket":"ticket"}""");
        using var httpClient = new HttpClient(handler);
        var api = new WeixinClawApi(httpClient);
        var expected = new JsonObject
        {
            ["base_info"] = new JsonObject
            {
                ["channel_version"] = "2.4.9", ["bot_agent"] = "NCF-WeixinManager/2.4.9"
            }
        };
        switch (operation)
        {
            case "getconfig":
                await api.GetConfigAsync("https://ilink.example.test", "bot-secret", "user", "ctx");
                expected["ilink_user_id"] = "user";
                expected["context_token"] = "ctx";
                break;
            case "sendtyping":
                await api.SendTypingAsync("https://ilink.example.test", "bot-secret", "user", "ticket", 1);
                expected["ilink_user_id"] = "user";
                expected["typing_ticket"] = "ticket";
                expected["status"] = 1;
                break;
            case "notifystart":
                await api.NotifyStartAsync("https://ilink.example.test", "bot-secret");
                break;
            case "notifystop":
                await api.NotifyStopAsync("https://ilink.example.test", "bot-secret");
                break;
        }

        AssertJsonBody(expected.ToJsonString(), handler.Body);
        Assert.AreEqual(
            "/ilink/bot/" + (operation.StartsWith("notify", StringComparison.Ordinal) ? "msg/" : "") + operation,
            handler.Request.RequestUri.AbsolutePath);
        AssertPostHeaders(handler.Request);
    }

    private static void AssertJsonBody(string expected, string actual)
    {
        Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)),
            $"Expected JSON: {expected}{Environment.NewLine}Actual JSON: {actual}");
    }

    private static void AssertPostHeaders(HttpRequestMessage request)
    {
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("application/json", request.Content.Headers.ContentType?.ToString());
        Assert.AreEqual("ilink_bot_token", request.Headers.GetValues("AuthorizationType").Single());
        Assert.AreEqual("bot", request.Headers.GetValues("iLink-App-Id").Single());
        Assert.AreEqual("132105", request.Headers.GetValues("iLink-App-ClientVersion").Single());
        Assert.AreEqual("Bearer", request.Headers.Authorization?.Scheme);
        Assert.AreEqual("bot-secret", request.Headers.Authorization?.Parameter);
        var uin = Encoding.UTF8.GetString(Convert.FromBase64String(
            request.Headers.GetValues("X-WECHAT-UIN").Single()));
        Assert.IsTrue(uint.TryParse(uin, NumberStyles.None, CultureInfo.InvariantCulture, out _));
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
