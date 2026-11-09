/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：AiKernelChatClients.cs
    文件功能描述：由 AI Kernel 模型（AIModelDto）构建并缓存对话客户端（IChatClient）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 新增 AiKernelChatClients：支持 NeuCharAI、AzureOpenAI、OpenAI、DeepSeek 的 Chat 模型，替代 NeuChar 环境变量密钥

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 OpenAI、DeepSeek 的模型名只用 ModelId（与 AI Kernel 一致），DeploymentName 只用于 NeuCharAI、AzureOpenAI

----------------------------------------------------------------*/

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Security.Cryptography;
using System.Text;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using Senparc.AI;
using Senparc.AI.AgentKernel.Kernels;
using Senparc.AI.AgentKernel.Kernels.KernelBuilderExtensions;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.NeuCharBoxLive.Api.Model;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Domain.Services;

/// <summary>What <see cref="AiKernelChatClients.Resolve"/> found: a model to call, or why there is none (a <see cref="LiveOff"/> code).</summary>
public sealed record ModelResolution(LiveModel? Model, string? OffReason);

/// <summary>
/// Chat clients for AI Kernel models (table <c>Senparc_AIKernel_AIModel</c>), built through Senparc.AI.AgentKernel's
/// builders and kept per model configuration. A singleton: the host resolves the plan and run models through it.
/// <para>
/// Fields are used the way AI Kernel's <c>AIModelService.BuildSenparcAiSetting</c> uses them: NeuCharAI and AzureOpenAI take
/// Endpoint (NeuCharAI's gets a trailing slash, so its developer-id path survives), ApiKey and the deployment; OpenAI takes
/// ApiKey, the model and OrganizationId (no endpoint); DeepSeek takes Endpoint, ApiKey and the model.
/// <see cref="LiveModel.Name"/> (which the engine also sends as ChatOptions.ModelId) is the deployment for NeuCharAI and
/// AzureOpenAI: DeploymentName when it is set, else ModelId. For OpenAI and DeepSeek it is the model, ModelId, as in AI
/// Kernel; their DeploymentName is ignored.
/// </para>
/// <para>
/// Every client runs on this module's transport: <see cref="ConnectRetryHandler"/> (<see cref="ConnectRetries"/> connect
/// retries, <see cref="ResetRetries"/> reset retries) over a SocketsHttpHandler (<see cref="NewSockets"/>), a
/// <see cref="RequestTimeout"/> HttpClient timeout, and the SDK's own retries off, so a request that got an answer is never
/// resent. The key is never logged and never part of a cache key, an exception message or an off reason.
/// </para>
/// </summary>
/// <param name="inner">Replaces the SocketsHttpHandler (tests); called once per client built, the retry handler still wraps it.</param>
/// <param name="logger">Where a model that can't be built is reported (type and message, the key replaced by ***).</param>
public sealed class AiKernelChatClients(Func<HttpMessageHandler>? inner = null, ILogger<AiKernelChatClients>? logger = null)
{
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(6);
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(90);

    /// <summary>Retries when no connection was made. About half the TLS handshakes to neuchar.com stall (2026-11-07).</summary>
    public const int ConnectRetries = 6;

    /// <summary>Retries when the service resets the connection before the response (10054 mid-call, 2026-11-08).</summary>
    public const int ResetRetries = 2;

    /// <summary>
    /// The Azure-style API version sent when the model's ApiVersion is blank or not one the SDK knows: the one AgentKernel
    /// itself always sends (AI Kernel's own blank default, 2022-12-01, is older than any the SDK can send).
    /// </summary>
    internal const AzureOpenAIClientOptions.ServiceVersion DefaultServiceVersion = AzureOpenAIClientOptions.ServiceVersion.V2025_04_01_Preview;

    readonly object _gate = new();

    /// <summary>Built models by configuration; null when that configuration can't be built (logged once).</summary>
    readonly Dictionary<CacheKey, LiveModel?> _models = new();

    /// <summary>The configuration each AI Kernel model Id last resolved with, so an edited model's old client is dropped.</summary>
    readonly Dictionary<int, CacheKey> _byId = new();

    /// <summary>The platforms live can call: NeuCharAI, AzureOpenAI, OpenAI and DeepSeek.</summary>
    public static bool IsSupported(AiPlatform p) =>
        p is AiPlatform.NeuCharAI or AiPlatform.AzureOpenAI or AiPlatform.OpenAI or AiPlatform.DeepSeek;

    /// <summary>
    /// The model for <paramref name="model"/>: <see cref="LiveOff.ModelMissing"/> when it is null (the chosen Id is gone),
    /// <see cref="LiveOff.ModelUnsupported"/> when it isn't a Chat model, its platform isn't supported, or its client can't
    /// be built (bad endpoint, no key, the SDK throws). The same configuration always gets the same client; any change to
    /// it (platform, endpoint, deployment, model id, API version, organization, key) builds a new one. Never throws.
    /// </summary>
    public ModelResolution Resolve(AIModelDto? model)
    {
        if (model is null) return new(null, LiveOff.ModelMissing);
        if (model.ConfigModelType != ConfigModelType.Chat || !IsSupported(model.AiPlatform)) return new(null, LiveOff.ModelUnsupported);

        var key = CacheKeyOf(model);
        lock (_gate)
        {
            if (!_models.TryGetValue(key, out var live))
            {
                try
                {
                    live = Build(model);
                }
                catch (Exception e)
                {
                    live = null;
                    logger?.LogWarning("Live can't use AI Kernel model {AiModelId} ({AiPlatform}): {ExceptionType}: {Message}",
                        model.Id, model.AiPlatform, e.GetType().FullName, Scrub(e.Message, model.ApiKey));
                }
                _models[key] = live;
            }
            Remember(model.Id, key);
            return live is null ? new(null, LiveOff.ModelUnsupported) : new(live, null);
        }
    }

    /// <summary>Points <paramref name="id"/> at <paramref name="key"/> and drops its previous configuration unless another Id uses it.</summary>
    void Remember(int id, CacheKey key)
    {
        if (_byId.TryGetValue(id, out var old) && old == key) return;
        _byId[id] = key;
        if (old is not null && !_byId.ContainsValue(old)) _models.Remove(old);
    }

    LiveModel Build(AIModelDto m)
    {
        var name = NameOf(m);
        if (name.Length == 0) throw new ArgumentException("The model has no ModelId (nor, for NeuCharAI and AzureOpenAI, a DeploymentName).");
        var apiKey = Clean(m.ApiKey);
        if (apiKey.Length == 0) throw new ArgumentException("The model has no ApiKey.");

        var builder = AIKernelBuilder.CreateBuilder();
        OpenAI.Chat.ChatClient chat = m.AiPlatform switch
        {
            AiPlatform.NeuCharAI => builder.AddNeuCharAIChatCompletion(NeuCharEndpoint(m.Endpoint), new ApiKeyCredential(apiKey),
                AzureOptions(m.ApiVersion), name),
            AiPlatform.AzureOpenAI => builder.AddAzureOpenAIChatCompletion(HttpEndpoint(m.Endpoint), new ApiKeyCredential(apiKey),
                AzureOptions(m.ApiVersion), name),
            AiPlatform.OpenAI => builder.AddOpenAIChatCompletion(apiKey, name, OpenAIOptions(m.OrganizationId)),
            AiPlatform.DeepSeek => builder.AddDeepSeekChatCompletion(apiKey, name, HttpEndpoint(m.Endpoint).OriginalString, OpenAIOptions(null)),
            _ => throw new NotSupportedException($"AI platform {m.AiPlatform} is not supported."),
        };
        return new LiveModel(name, chat.AsIChatClient());
    }

    /// <summary>
    /// The deployment for the deployment-addressed platforms (NeuCharAI, AzureOpenAI): DeploymentName when it is set, else
    /// ModelId. The model for OpenAI and DeepSeek: ModelId, as AI Kernel sends it. Trimmed; empty when there is none.
    /// </summary>
    static string NameOf(AIModelDto m) =>
        (m.AiPlatform is AiPlatform.NeuCharAI or AiPlatform.AzureOpenAI) && !string.IsNullOrWhiteSpace(m.DeploymentName)
            ? m.DeploymentName.Trim()
            : Clean(m.ModelId);

    AzureOpenAIClientOptions AzureOptions(string? apiVersion) => new(ServiceVersionOf(apiVersion))
    {
        Transport = NewTransport(),
        RetryPolicy = new ClientRetryPolicy(0),
    };

    OpenAIClientOptions OpenAIOptions(string? organizationId) => new()
    {
        Transport = NewTransport(),
        RetryPolicy = new ClientRetryPolicy(0),
        OrganizationId = string.IsNullOrWhiteSpace(organizationId) ? null : organizationId.Trim(),
    };

    HttpClientPipelineTransport NewTransport() =>
        new(new HttpClient(new ConnectRetryHandler(ConnectRetries, resetRetries: ResetRetries) { InnerHandler = inner?.Invoke() ?? NewSockets() })
        {
            Timeout = RequestTimeout,
        });

    /// <summary>
    /// Keep-alive (pooled connections, reused for 90 s idle, renewed every 10 min), a 6 s connect timeout, and no TLS
    /// session resumption: a request on a resumed session to neuchar.com stalls until the timeout about half the time
    /// (2026-11-07), while full handshakes don't. NeuChar answers <c>Connection: close</c>, so every call is a new
    /// connection and the extra handshake costs one round trip.
    /// </summary>
    internal static SocketsHttpHandler NewSockets() => new()
    {
        ConnectTimeout = ConnectTimeout,
        PooledConnectionIdleTimeout = TimeSpan.FromSeconds(90),
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        SslOptions = new System.Net.Security.SslClientAuthenticationOptions { AllowTlsResume = false },
    };

    /// <summary>The model's ApiVersion when the SDK knows it (2024-10-21 → V2024_10_21), else <see cref="DefaultServiceVersion"/>.</summary>
    internal static AzureOpenAIClientOptions.ServiceVersion ServiceVersionOf(string? apiVersion)
    {
        var wanted = "V" + Clean(apiVersion).Replace('-', '_');
        foreach (var v in Enum.GetValues<AzureOpenAIClientOptions.ServiceVersion>())
            if (string.Equals(v.ToString(), wanted, StringComparison.OrdinalIgnoreCase)) return v;
        return DefaultServiceVersion;
    }

    /// <summary>The NeuChar endpoint with a trailing slash, so the developer id in its path (/19194/) survives the SDK's URL join.</summary>
    static Uri NeuCharEndpoint(string? endpoint)
    {
        var u = HttpEndpoint(endpoint);
        return u.AbsolutePath.EndsWith('/') ? u : new Uri(u.GetLeftPart(UriPartial.Path) + "/" + u.Query);
    }

    /// <summary>The trimmed endpoint as an absolute http(s) URL. The message never repeats the endpoint (it could hold a key).</summary>
    static Uri HttpEndpoint(string? endpoint)
    {
        if (!Uri.TryCreate(Clean(endpoint), UriKind.Absolute, out var u) || (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("The model's Endpoint must be an absolute http(s) URL.");
        return u;
    }

    static string Clean(string? s) => s?.Trim() ?? "";

    /// <summary><paramref name="message"/> with every occurrence of the (trimmed) key replaced by <c>***</c>.</summary>
    internal static string Scrub(string message, string? apiKey)
    {
        var k = apiKey?.Trim();
        return string.IsNullOrEmpty(k) ? message : message.Replace(k, "***", StringComparison.Ordinal);
    }

    /// <summary>What identifies one client: every field it is built from, with a SHA-256 of the key in place of the key.</summary>
    internal sealed record CacheKey(AiPlatform Platform, string Endpoint, string Deployment, string ModelId, string ApiVersion,
        string Organization, string KeySha256);

    internal static CacheKey CacheKeyOf(AIModelDto m) => new(m.AiPlatform, Clean(m.Endpoint), Clean(m.DeploymentName), Clean(m.ModelId),
        Clean(m.ApiVersion), Clean(m.OrganizationId), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Clean(m.ApiKey)))));
}
