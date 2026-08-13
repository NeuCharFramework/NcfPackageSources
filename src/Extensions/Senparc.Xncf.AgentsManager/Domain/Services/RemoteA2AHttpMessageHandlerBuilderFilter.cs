/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：RemoteA2AHttpMessageHandlerBuilderFilter.cs
    文件功能描述：确保远程 A2A 调用使用 Agent 自身配置的超时边界


    创建标识：Senparc - 20260814

----------------------------------------------------------------*/

using Microsoft.Extensions.Http;
using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// 去除宿主通过 ConfigureHttpClientDefaults 为 AgentsManager A2A 客户端追加的
    /// 固定 Polly 超时/重试管道。A2A 的 POST 可能已在远程开始执行，不应被透明重试；
    /// 整个请求的超时由 RemoteAgent.TimeoutSeconds 统一控制。
    /// </summary>
    internal sealed class RemoteA2AHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        private const string ResilienceHandlerTypeName =
            "Microsoft.Extensions.Http.Resilience.ResilienceHandler";

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            ArgumentNullException.ThrowIfNull(next);

            return builder =>
            {
                // 先让宿主的 ConfigureHttpClientDefaults 和其他 Filter 完成。
                // 这使得无论 AddServiceDefaults 与 XNCF 模块的注册先后如何，
                // 都能在最终 Handler 链中去除固定 30 秒的 ResilienceHandler。
                next(builder);

                if (!string.Equals(
                        builder.Name,
                        RemoteA2AAgentFactory.HttpClientName,
                        StringComparison.Ordinal))
                {
                    return;
                }

                for (var index = builder.AdditionalHandlers.Count - 1; index >= 0; index--)
                {
                    if (string.Equals(
                            builder.AdditionalHandlers[index].GetType().FullName,
                            ResilienceHandlerTypeName,
                            StringComparison.Ordinal))
                    {
                        builder.AdditionalHandlers.RemoveAt(index);
                    }
                }
            };
        }
    }
}
