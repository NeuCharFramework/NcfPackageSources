using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Tests.WeixinClaw;

[TestClass]
public class WeixinClawLoginServiceTests : BaseTest
{
    [TestMethod]
    public async Task PollAsync_UsesRedirectHostForSubsequentRequests()
    {
        var handler = new SequencedHandler(
            """{"qrcode":"qr-1","qrcode_img_content":"data:image/png;base64,AA=="}""",
            """{"status":"scanned_but_redirect","redirect_host":"redirect-host.example.test"}""",
            """{"status":"wait"}""");
        using var httpClient = new HttpClient(handler);
        var loginService = new WeixinClawLoginService(
            new WeixinClawApi(httpClient),
            ServiceProvider.GetRequiredService<IServiceScopeFactory>());

        var start = await loginService.StartAsync("个人微信", "prompt-range");
        var statuses = await Task.WhenAll(
            loginService.PollAsync(start.SessionId),
            loginService.PollAsync(start.SessionId));

        CollectionAssert.AreEqual(
            new[] { "ilinkai.weixin.qq.com", "ilinkai.weixin.qq.com", "redirect-host.example.test" },
            handler.Requests.Select(uri => uri.Host).ToArray());
        Assert.IsTrue(statuses.Any(z => z.Status == "scanned_but_redirect"));
        Assert.IsTrue(statuses.Any(z => z.Status == "wait"));
    }

    private sealed class SequencedHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses;
        private readonly object _syncRoot = new();

        public SequencedHandler(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public List<System.Uri> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string response;
            lock (_syncRoot)
            {
                Requests.Add(request.RequestUri);
                response = _responses.Dequeue();
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }
}
