/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AiKernelClientTests.cs
    文件功能描述：NeuCharBox Live 测试：由 AI Kernel 模型构建对话客户端（AiKernelChatClients）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增 AiKernelChatClients 的测试（解析、缓存、四个平台的请求地址、密钥不外泄）

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 OpenAI、DeepSeek 的模型名为 ModelId，即使填写了 DeploymentName

----------------------------------------------------------------*/

using System.ClientModel;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Senparc.AI;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.NeuCharBoxLive.Domain.Services;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;
using Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// Chat clients built from AI Kernel models, without the network: every client here gets a fake inner handler (or none
/// is called), and the key is a made-up one.
/// </summary>
[TestClass]
public class AiKernelClientTests
{
    const string FakeKey = "sk-SECRET-123";

    const string Completion = """
        {"id":"chatcmpl-1","object":"chat.completion","created":1762500000,"model":"gpt-5-chat",
         "choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}],
         "usage":{"prompt_tokens":3,"completion_tokens":1,"total_tokens":4}}
        """;

    /// <summary>A model as AI Kernel stores it: a Chat model on <paramref name="p"/>, with an endpoint where the platform takes one.</summary>
    static AIModelDto Dto(AiPlatform p, string? deployment = "gpt-5-chat", string model = "gpt-5-chat-model", int id = 7) => new()
    {
        Id = id,
        Alias = "live",
        AiPlatform = p,
        ConfigModelType = ConfigModelType.Chat,
        Endpoint = p switch
        {
            AiPlatform.NeuCharAI => "https://www.neuchar.com/19194/",
            AiPlatform.AzureOpenAI => "https://example-res.openai.azure.com/",
            AiPlatform.DeepSeek => "https://api.deepseek.com",
            _ => "",
        },
        DeploymentName = deployment!,
        ModelId = model,
        ApiKey = FakeKey,
        ApiVersion = "2025-04-01-preview",
    };

    /// <summary>A copy, so a test can edit one field and compare.</summary>
    static AIModelDto Copy(AIModelDto d) => new()
    {
        Id = d.Id, Alias = d.Alias, AiPlatform = d.AiPlatform, ConfigModelType = d.ConfigModelType, Endpoint = d.Endpoint,
        DeploymentName = d.DeploymentName, ModelId = d.ModelId, ApiKey = d.ApiKey, ApiVersion = d.ApiVersion,
        OrganizationId = d.OrganizationId,
    };

    /// <summary>Each attempt takes the next step (the last one repeats). Records the method, the URL and which headers came.</summary>
    sealed class StepHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] steps) : HttpMessageHandler
    {
        readonly List<(HttpMethod Method, Uri? Uri, HashSet<string> Headers)> _requests = new();

        public IReadOnlyList<(HttpMethod Method, Uri? Uri, HashSet<string> Headers)> Requests { get { lock (_requests) return _requests.ToList(); } }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            int i;
            lock (_requests)
            {
                // Header names only: the values (the key among them) are never read.
                _requests.Add((request.Method, request.RequestUri, request.Headers.Select(h => h.Key).ToHashSet(StringComparer.OrdinalIgnoreCase)));
                i = _requests.Count - 1;
            }
            try { return Task.FromResult(steps[Math.Min(i, steps.Length - 1)](request)); }
            catch (Exception e) { return Task.FromException<HttpResponseMessage>(e); }
        }
    }

    static Func<HttpRequestMessage, HttpResponseMessage> Status(HttpStatusCode code, string body = Completion) =>
        _ => new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    static Func<HttpRequestMessage, HttpResponseMessage> ConnectFailure =>
        _ => throw new HttpRequestException(HttpRequestError.ConnectionError, "fake connect failure");

    // ---- what resolves to what ----

    [TestMethod]
    public void Resolve_Null_IsModelMissing()
    {
        var r = new AiKernelChatClients(() => new StepHandler(Status(HttpStatusCode.OK))).Resolve(null);
        Assert.IsNull(r.Model);
        Assert.AreEqual(LiveOff.ModelMissing, r.OffReason);
    }

    [TestMethod]
    public void Resolve_NotChatType_IsUnsupported()
    {
        var clients = new AiKernelChatClients(() => new StepHandler(Status(HttpStatusCode.OK)));
        foreach (var type in new[] { ConfigModelType.TextEmbedding, ConfigModelType.TextCompletion, ConfigModelType.TextToImage })
        {
            var d = Dto(AiPlatform.NeuCharAI);
            d.ConfigModelType = type;
            var r = clients.Resolve(d);
            Assert.IsNull(r.Model, type.ToString());
            Assert.AreEqual(LiveOff.ModelUnsupported, r.OffReason, type.ToString());
        }
    }

    [TestMethod]
    public void Resolve_UnsupportedPlatform_IsUnsupported()
    {
        var clients = new AiKernelChatClients(() => new StepHandler(Status(HttpStatusCode.OK)));
        foreach (var p in new[] { AiPlatform.HuggingFace, AiPlatform.Ollama, AiPlatform.FastAPI })
        {
            var r = clients.Resolve(Dto(p));
            Assert.IsNull(r.Model, p.ToString());
            Assert.AreEqual(LiveOff.ModelUnsupported, r.OffReason, p.ToString());
            Assert.IsFalse(AiKernelChatClients.IsSupported(p), p.ToString());
        }
        foreach (var p in new[] { AiPlatform.NeuCharAI, AiPlatform.AzureOpenAI, AiPlatform.OpenAI, AiPlatform.DeepSeek })
            Assert.IsTrue(AiKernelChatClients.IsSupported(p), p.ToString());
    }

    [DataTestMethod]
    [DataRow("NeuCharAI")]
    [DataRow("AzureOpenAI")]
    [DataRow("OpenAI")]
    [DataRow("DeepSeek")]
    public void Resolve_SupportedPlatform_BuildsAClient(string platform)
    {
        var p = Enum.Parse<AiPlatform>(platform);
        var clients = new AiKernelChatClients(() => new StepHandler(Status(HttpStatusCode.OK)));

        // NeuCharAI and AzureOpenAI address a deployment: DeploymentName when there is one. OpenAI and DeepSeek name the
        // model: ModelId, as AI Kernel sends it, even when a DeploymentName is filled in.
        var deploymentAddressed = p is AiPlatform.NeuCharAI or AiPlatform.AzureOpenAI;
        var withDeployment = clients.Resolve(Dto(p, deployment: "dep-1", model: "model-1", id: 1));
        Assert.IsNull(withDeployment.OffReason);
        Assert.IsNotNull(withDeployment.Model);
        Assert.IsNotNull(withDeployment.Model.Client);
        Assert.AreEqual(deploymentAddressed ? "dep-1" : "model-1", withDeployment.Model.Name,
            deploymentAddressed ? "DeploymentName when there is one" : "ModelId; DeploymentName is ignored");

        foreach (var blank in new[] { null, "", "   " })
        {
            var noDeployment = clients.Resolve(Dto(p, deployment: blank, model: "model-1", id: 2));
            Assert.IsNull(noDeployment.OffReason, $"[{blank}]");
            Assert.AreEqual("model-1", noDeployment.Model!.Name, $"ModelId when DeploymentName is [{blank}]");
        }
    }

    // ---- the cache ----

    [TestMethod]
    public void Resolve_SameModel_ReturnsCachedClient()
    {
        var clients = new AiKernelChatClients(() => new StepHandler(Status(HttpStatusCode.OK)));
        var d = Dto(AiPlatform.NeuCharAI);
        var first = clients.Resolve(d);
        var second = clients.Resolve(d);
        var equalCopy = clients.Resolve(Copy(d));
        Assert.IsTrue(ReferenceEquals(first.Model!.Client, second.Model!.Client), "same model, same client");
        Assert.IsTrue(ReferenceEquals(first.Model.Client, equalCopy.Model!.Client), "an equal row read again, same client");
    }

    [TestMethod]
    public void Resolve_EditedKeyOrEndpoint_BuildsNewClient()
    {
        var clients = new AiKernelChatClients(() => new StepHandler(Status(HttpStatusCode.OK)));
        var d = Dto(AiPlatform.NeuCharAI);
        var before = clients.Resolve(d).Model!.Client;

        var newKey = Copy(d);
        newKey.ApiKey = "sk-SECRET-456";
        var afterKey = clients.Resolve(newKey).Model!.Client;
        Assert.IsFalse(ReferenceEquals(before, afterKey), "a new key builds a new client");

        var newEndpoint = Copy(newKey);
        newEndpoint.Endpoint = "https://www.neuchar.com/20000/";
        var afterEndpoint = clients.Resolve(newEndpoint).Model!.Client;
        Assert.IsFalse(ReferenceEquals(afterKey, afterEndpoint), "a new endpoint builds a new client");

        // Every other part of the key too: deployment, model id, API version, organization, platform.
        var edits = new Action<AIModelDto>[]
        {
            x => x.DeploymentName = "gpt-5-mini",
            x => x.ModelId = "other-model",
            x => x.ApiVersion = "2024-10-21",
            x => x.OrganizationId = "org-1",
            x => x.AiPlatform = AiPlatform.AzureOpenAI,
        };
        var last = afterEndpoint;
        var current = newEndpoint;
        foreach (var edit in edits)
        {
            current = Copy(current);
            edit(current);
            var next = clients.Resolve(current).Model!.Client;
            Assert.IsFalse(ReferenceEquals(last, next));
            last = next;
        }
    }

    [TestMethod]
    public void CacheKey_CarriesAHashOfTheKey_NeverTheKey()
    {
        var key = AiKernelChatClients.CacheKeyOf(Dto(AiPlatform.NeuCharAI)).ToString()!;
        Assert.IsFalse(key.Contains(FakeKey), key);
        Assert.IsFalse(key.Contains("SECRET"), key);
        StringAssert.Matches(key, new Regex("[0-9a-f]{64}"), "a SHA-256 of the key");
    }

    // ---- the requests each platform makes ----

    [TestMethod]
    public async Task Resolve_NeuCharAI_PostsToDeployment_WithApiKeyHeader()
    {
        foreach (var endpoint in new[] { "https://www.neuchar.com/19194/", "https://www.neuchar.com/19194", "  https://www.neuchar.com/19194/ " })
        {
            var inner = new StepHandler(Status(HttpStatusCode.OK));
            var d = Dto(AiPlatform.NeuCharAI, deployment: "gpt-5-chat");
            d.Endpoint = endpoint;
            var client = new AiKernelChatClients(() => inner).Resolve(d).Model!.Client;

            var resp = await client.GetResponseAsync("hi");

            Assert.AreEqual("ok", resp.Text);
            var req = inner.Requests.Single();
            Assert.AreEqual(HttpMethod.Post, req.Method);
            Assert.AreEqual("https://www.neuchar.com/19194/openai/deployments/gpt-5-chat/chat/completions?api-version=2025-04-01-preview",
                req.Uri?.ToString(), endpoint);
            Assert.IsTrue(req.Headers.Contains("api-key"), "api-key header present");
            Assert.IsFalse(req.Headers.Contains("Authorization"), "Azure style: no bearer header");
        }
    }

    [DataTestMethod]
    [DataRow("NeuCharAI", "https://www.neuchar.com/19194/openai/deployments/dep-1/chat/completions?api-version=2025-04-01-preview", "api-key")]
    [DataRow("AzureOpenAI", "https://example-res.openai.azure.com/openai/deployments/dep-1/chat/completions?api-version=2025-04-01-preview", "api-key")]
    [DataRow("OpenAI", "https://api.openai.com/v1/chat/completions", "Authorization")]
    [DataRow("DeepSeek", "https://api.deepseek.com/chat/completions", "Authorization")]
    public async Task Resolve_EachPlatform_CallsThroughTheRetryTransport(string platform, string url, string keyHeader)
    {
        // The first attempt fails to connect: the retry handler sends it again, so the transport is the module's.
        var inner = new StepHandler(ConnectFailure, Status(HttpStatusCode.OK));
        var d = Dto(Enum.Parse<AiPlatform>(platform), deployment: "dep-1");
        var client = new AiKernelChatClients(() => inner).Resolve(d).Model!.Client;

        var resp = await client.GetResponseAsync("hi");

        Assert.AreEqual("ok", resp.Text);
        Assert.AreEqual(2, inner.Requests.Count, "one connect retry");
        Assert.IsTrue(inner.Requests.All(r => r.Method == HttpMethod.Post && r.Uri?.ToString() == url), string.Join(", ", inner.Requests.Select(r => r.Uri)));
        Assert.IsTrue(inner.Requests[1].Headers.Contains(keyHeader), keyHeader);
    }

    [TestMethod]
    public async Task Resolve_OpenAI_SendsTheOrganization()
    {
        var inner = new StepHandler(Status(HttpStatusCode.OK));
        var d = Dto(AiPlatform.OpenAI, deployment: null);
        d.OrganizationId = "org-123";
        await new AiKernelChatClients(() => inner).Resolve(d).Model!.Client.GetResponseAsync("hi");
        Assert.IsTrue(inner.Requests.Single().Headers.Contains("OpenAI-Organization"));
    }

    [TestMethod]
    public async Task Resolve_ApiVersion_KnownOnesAreUsed_OthersFallBackToTheAgentKernelDefault()
    {
        // AI Kernel's own default for a blank version (2022-12-01) is older than any version the SDK can send; AgentKernel
        // itself always sends 2025-04-01-preview, so that is the fallback.
        var cases = new (string? Version, string Sent)[]
        {
            ("2024-10-21", "2024-10-21"),
            ("2025-01-01-preview", "2025-01-01-preview"),
            (" 2024-06-01 ", "2024-06-01"),
            (null, "2025-04-01-preview"),
            ("", "2025-04-01-preview"),
            ("2022-12-01", "2025-04-01-preview"),
            ("9", "2025-04-01-preview"),
            ("2024-06-01,2024-10-21", "2025-04-01-preview"),
        };
        foreach (var (version, sent) in cases)
        {
            var inner = new StepHandler(Status(HttpStatusCode.OK));
            var d = Dto(AiPlatform.AzureOpenAI, deployment: "dep-1");
            d.ApiVersion = version!;
            await new AiKernelChatClients(() => inner).Resolve(d).Model!.Client.GetResponseAsync("hi");
            StringAssert.EndsWith(inner.Requests.Single().Uri!.ToString(), "?api-version=" + sent, $"[{version}]");
        }
    }

    [DataTestMethod]
    [DataRow("NeuCharAI")]
    [DataRow("AzureOpenAI")]
    [DataRow("OpenAI")]
    [DataRow("DeepSeek")]
    public async Task Resolve_ServerErrors_AreNeverRetried(string platform)
    {
        // The SDK's own retry policy would resend a 429 or 5xx: it is off, so a request that got an answer goes once.
        foreach (var code in new[] { HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable, HttpStatusCode.TooManyRequests, HttpStatusCode.Forbidden })
        {
            var inner = new StepHandler(Status(code, """{"error":{"message":"AI 应用不可用或已暂时停用"}}"""), Status(HttpStatusCode.OK));
            var client = new AiKernelChatClients(() => inner).Resolve(Dto(Enum.Parse<AiPlatform>(platform))).Model!.Client;
            var e = await Assert.ThrowsExceptionAsync<ClientResultException>(() => client.GetResponseAsync("hi"), code.ToString());
            Assert.AreEqual((int)code, e.Status);
            Assert.AreEqual(1, inner.Requests.Count, code.ToString());
            Assert.IsFalse(e.ToString().Contains(FakeKey), code.ToString());
        }
    }

    // ---- models that can't be built, and the key ----

    [TestMethod]
    public void FactoryError_DoesNotContainKey()
    {
        var bad = new List<(string Name, AIModelDto Model)>();
        foreach (var p in new[] { AiPlatform.NeuCharAI, AiPlatform.AzureOpenAI, AiPlatform.DeepSeek })
        {
            foreach (var endpoint in new[] { "not a url", "", "/19194/", "ftp://www.neuchar.com/19194/", FakeKey })
            {
                var d = Dto(p);
                d.Endpoint = endpoint;
                bad.Add(($"{p} endpoint [{endpoint}]", d));
            }
        }
        foreach (var p in new[] { AiPlatform.NeuCharAI, AiPlatform.AzureOpenAI, AiPlatform.OpenAI, AiPlatform.DeepSeek })
        {
            var noName = Dto(p, deployment: "", model: "");
            bad.Add(($"{p} no deployment or model id", noName));
            if (p is AiPlatform.OpenAI or AiPlatform.DeepSeek)
                bad.Add(($"{p} a deployment but no model id", Dto(p, deployment: "dep-1", model: "")));
            foreach (var blank in new[] { null, "", "  " })
            {
                var noKey = Dto(p);
                noKey.ApiKey = blank!;
                bad.Add(($"{p} key [{blank}]", noKey));
            }
        }

        foreach (var (name, model) in bad)
        {
            var log = new ListLogger<AiKernelChatClients>();
            var clients = new AiKernelChatClients(() => new StepHandler(Status(HttpStatusCode.OK)), log);
            ModelResolution? r = null;
            Exception? thrown = null;
            try { r = clients.Resolve(model); }
            catch (Exception e) { thrown = e; }

            Assert.IsNull(thrown, $"{name}: Resolve never throws, it turns live off ({thrown?.GetType().Name})");
            Assert.IsNull(r!.Model, name);
            Assert.AreEqual(LiveOff.ModelUnsupported, r.OffReason, name);
            Assert.IsFalse(r.ToString().Contains(FakeKey), name);
            Assert.AreEqual(1, log.Entries.Count, $"{name}: one log line");
            var line = log.Entries.Single().Message;
            StringAssert.Contains(line, "Exception", $"{name}: the line names the exception type");
            StringAssert.Contains(line, model.Id.ToString(), $"{name}: and the model");
            Assert.IsFalse(line.Contains(FakeKey), $"{name}: {line}");
            Assert.IsFalse(line.Contains("SECRET"), $"{name}: {line}");
        }
    }

    [TestMethod]
    public void FactoryError_IsLoggedOnce_UntilTheModelChanges()
    {
        var log = new ListLogger<AiKernelChatClients>();
        var clients = new AiKernelChatClients(() => new StepHandler(Status(HttpStatusCode.OK)), log);
        var d = Dto(AiPlatform.NeuCharAI);
        d.Endpoint = "not a url";

        Assert.AreEqual(LiveOff.ModelUnsupported, clients.Resolve(d).OffReason);
        Assert.AreEqual(LiveOff.ModelUnsupported, clients.Resolve(Copy(d)).OffReason);
        Assert.AreEqual(1, log.Entries.Count, "the same configuration is reported once");

        var fixedModel = Copy(d);
        fixedModel.Endpoint = "https://www.neuchar.com/19194/";
        Assert.IsNotNull(clients.Resolve(fixedModel).Model, "fixing the endpoint builds the client");
        Assert.AreEqual(1, log.Entries.Count);
    }

    [TestMethod]
    public void Scrub_ReplacesEveryOccurrenceOfTheKey()
    {
        Assert.AreEqual("bad *** and *** again", AiKernelChatClients.Scrub($"bad {FakeKey} and {FakeKey} again", FakeKey));
        Assert.AreEqual("bad *** here", AiKernelChatClients.Scrub($"bad {FakeKey} here", $"  {FakeKey}\n"), "the trimmed key too");
        Assert.AreEqual("nothing", AiKernelChatClients.Scrub("nothing", null));
        Assert.AreEqual("nothing", AiKernelChatClients.Scrub("nothing", "  "));
    }

    // ---- the transport ----

    [TestMethod]
    public void Transport_ConnectTimeoutAndKeepAlive()
    {
        using var s = AiKernelChatClients.NewSockets();
        // About half the handshakes to neuchar.com stall; good ones finish in under 1.5 s. Seven tries of 6 s plus the
        // 9.5 s of backoff stay well inside the 90 s request timeout.
        Assert.AreEqual(TimeSpan.FromSeconds(6), s.ConnectTimeout);
        Assert.AreEqual(6, AiKernelChatClients.ConnectRetries);
        Assert.AreEqual(2, AiKernelChatClients.ResetRetries);
        Assert.AreEqual(TimeSpan.FromSeconds(90), AiKernelChatClients.RequestTimeout);
        Assert.IsTrue(AiKernelChatClients.ConnectTimeout * (AiKernelChatClients.ConnectRetries + 1)
            + TimeSpan.FromSeconds(9.5) < AiKernelChatClients.RequestTimeout - TimeSpan.FromSeconds(30), "room left for the model");
        Assert.AreEqual(TimeSpan.FromSeconds(90), s.PooledConnectionIdleTimeout, "idle connections stay open for reuse");
        Assert.AreEqual(TimeSpan.FromMinutes(10), s.PooledConnectionLifetime);
        Assert.IsTrue(s.MaxConnectionsPerServer > 1);
    }

    [TestMethod]
    public void Transport_NeverResumesTls()
    {
        // Resumed TLS sessions to neuchar.com stall until the timeout about half the time (2026-11-07: 7 of 12 resumed
        // requests stalled, 0 of 6 with resumption off, 0 of ~40 from curl, which never resumes).
        using var s = AiKernelChatClients.NewSockets();
        Assert.IsNotNull(s.SslOptions);
        Assert.IsFalse(s.SslOptions.AllowTlsResume, "every connection to the model does a full TLS handshake");
    }
}
