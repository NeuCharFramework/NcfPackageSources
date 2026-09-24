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
        StringAssert.Contains(handler.Body, "\"channel_version\":\"0.1.0\"");
        StringAssert.Contains(handler.Body, "\"bot_agent\":\"NCF-WeixinManager/0.1.0\"");
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
}
