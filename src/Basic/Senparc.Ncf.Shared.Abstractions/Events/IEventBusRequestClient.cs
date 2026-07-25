/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IEventBusRequestClient.cs
    文件功能描述：EventBus 请求-响应客户端公共接口


    创建标识：Senparc - 20260725

----------------------------------------------------------------*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// 在 EventBus 上发布请求并异步等待对应响应。
    /// </summary>
    /// <remarks>
    /// 实现必须先登记请求再发布事件，并在超时、取消或完成后释放等待项。
    /// 该接口只描述同一 EventBus 内的关联机制，不提供跨进程持久化或安全隔离。
    /// </remarks>
    public interface IEventBusRequestClient
    {
        /// <summary>
        /// 发布请求并等待匹配 <see cref="IIntegrationResponse.RequestId"/> 的指定响应类型。
        /// </summary>
        /// <typeparam name="TResponse">期望的响应事件类型。</typeparam>
        /// <param name="request">请求事件。</param>
        /// <param name="timeout">有限且大于零的等待时间。</param>
        /// <param name="cancellationToken">调用方取消令牌。</param>
        /// <returns>匹配的响应事件。</returns>
        Task<TResponse> RequestAsync<TResponse>(
            IIntegrationRequest<TResponse> request,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            where TResponse : class, IIntegrationResponse;
    }
}
