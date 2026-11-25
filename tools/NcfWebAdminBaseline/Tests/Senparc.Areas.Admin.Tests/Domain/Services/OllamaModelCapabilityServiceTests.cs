using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.Entities;
using Senparc.AI.Entities.Keys;
using Senparc.Areas.Admin.Domain.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class OllamaModelCapabilityServiceTests
{
    [TestMethod]
    [DataRow("[\"completion\",\"tools\"]", true)]
    [DataRow("[\"completion\",\"vision\"]", false)]
    public async Task SupportsToolsAsync_UsesOllamaCapabilities(
        string capabilities,
        bool expected)
    {
        HttpRequestMessage request = null;
        var handler = new StubHttpMessageHandler(message =>
        {
            request = message;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"capabilities\":{capabilities}}}",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(value => value.CreateClient(OllamaModelCapabilityService.HttpClientName))
            .Returns(new HttpClient(handler));
        var service = new OllamaModelCapabilityService(
            factory.Object,
            NullLogger<OllamaModelCapabilityService>.Instance);
        var setting = new SenparcAiSetting
        {
            AiPlatform = AiPlatform.Ollama,
            OllamaKeys = new OllamaKeys
            {
                Endpoint = "http://ollama.local:11434/v1/",
                ModelName = new ModelName { Chat = "qwen2.5vl:7b" }
            }
        };

        var result = await service.SupportsToolsAsync(setting, CancellationToken.None);

        Assert.AreEqual(expected, result);
        Assert.IsNotNull(request);
        Assert.AreEqual("http://ollama.local:11434/api/show", request.RequestUri.ToString());
    }

    [TestMethod]
    public async Task SupportsToolsAsync_ReturnsUnknownWhenMetadataCannotBeRead()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(value => value.CreateClient(OllamaModelCapabilityService.HttpClientName))
            .Returns(new HttpClient(new StubHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.InternalServerError))));
        var service = new OllamaModelCapabilityService(
            factory.Object,
            NullLogger<OllamaModelCapabilityService>.Instance);
        var setting = new SenparcAiSetting
        {
            AiPlatform = AiPlatform.Ollama,
            OllamaKeys = new OllamaKeys
            {
                Endpoint = "http://ollama.local:11434",
                ModelName = new ModelName { Chat = "qwen2.5vl:7b" }
            }
        };

        var result = await service.SupportsToolsAsync(setting, CancellationToken.None);

        Assert.IsNull(result);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_handler(request));
    }
}
