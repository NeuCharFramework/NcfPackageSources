/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharChatClientFactory.cs
    文件功能描述：NeuCharBox Live NeuChar 模型客户端：NeuCharChatClientFactory


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Senparc.AI.AgentKernel.Kernels;
using Senparc.AI.AgentKernel.Kernels.KernelBuilderExtensions;

namespace Senparc.Xncf.NeuCharBoxLive.NeuChar;

/// <summary>
/// An IChatClient for one NeuChar deployment, built through Senparc.AI's NeuCharAI platform (Azure-OpenAI style:
/// {endpoint}openai/deployments/{model}/chat/completions?api-version=2025-04-01-preview, header api-key).
/// </summary>
public static class NeuCharChatClientFactory
{
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(6);
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(90);

    /// <summary>Retries when no connection was made. About half the TLS handshakes to neuchar.com stall (2026-11-07).</summary>
    public const int ConnectRetries = 6;

    /// <summary>
    /// The client for deployment <c>o.Model</c>, or null when <paramref name="apiKey"/> is missing (live is then off).
    /// The host asks for one per model id and keeps it. Connect and handshake failures are retried up to <see cref="ConnectRetries"/> times by
    /// <see cref="ConnectRetryHandler"/>; the SDK's own retries are off, so a request that reached NeuChar is never resent.
    /// </summary>
    /// <param name="inner">Replaces the SocketsHttpHandler (tests); the retry handler still wraps it.</param>
    /// <exception cref="ArgumentException">The endpoint isn't an absolute http(s) URL, or the model is empty. The message
    /// never carries the key.</exception>
    public static IChatClient? Create(NeuCharOptions o, string? apiKey, HttpMessageHandler? inner = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var endpoint = EndpointOf(o);
        if (string.IsNullOrWhiteSpace(o.Model)) throw new ArgumentException("The NeuChar model (deployment) is empty.", nameof(o));

        var http = new HttpClient(new ConnectRetryHandler(ConnectRetries) { InnerHandler = inner ?? NewSockets() }) { Timeout = RequestTimeout };
        var opts = new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_04_01_Preview)
        {
            Transport = new HttpClientPipelineTransport(http),
            RetryPolicy = new ClientRetryPolicy(0),
        };
        OpenAI.Chat.ChatClient cc = AIKernelBuilder.CreateBuilder()
            .AddNeuCharAIChatCompletion(endpoint, new ApiKeyCredential(apiKey.Trim()), opts, o.Model);
        return cc.AsIChatClient();
    }

    /// <summary>
    /// Keep-alive (pooled connections, reused for 90 s idle, renewed every 10 min), a 10 s connect timeout, and no TLS
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

    /// <summary>The endpoint with a trailing slash, so the developer id in its path (/19194/) survives the SDK's URL join.</summary>
    static Uri EndpointOf(NeuCharOptions o)
    {
        if (!Uri.TryCreate(o.Endpoint, UriKind.Absolute, out var u) || (u.Scheme != Uri.UriSchemeHttps && u.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("The NeuChar endpoint must be an absolute http(s) URL.", nameof(o));
        return u.AbsolutePath.EndsWith('/') ? u : new Uri(u.GetLeftPart(UriPartial.Path) + "/" + u.Query);
    }
}
