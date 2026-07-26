/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IIntegrationRequest.cs
    文件功能描述：EventBus 请求-响应公共契约


    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v0.3.0-preview2 同步模块功能与兼容性改进

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// 可等待响应的集成事件请求。
    /// </summary>
    /// <typeparam name="TResponse">期望的响应事件类型。</typeparam>
    public interface IIntegrationRequest<out TResponse> : IIntegrationEvent
        where TResponse : class, IIntegrationResponse
    {
        /// <summary>
        /// 请求与响应之间的关联 ID。
        /// </summary>
        Guid RequestId { get; }
    }

    /// <summary>
    /// 可完成等待请求的集成事件响应。
    /// </summary>
    public interface IIntegrationResponse : IIntegrationEvent
    {
        /// <summary>
        /// 对应请求的关联 ID。
        /// </summary>
        Guid RequestId { get; }
    }

    /// <summary>
    /// 请求事件基类。默认生成不可预测的关联 ID，适合进程内请求-响应通信。
    /// </summary>
    /// <typeparam name="TResponse">期望的响应事件类型。</typeparam>
    public abstract record IntegrationRequest<TResponse> : IntegrationEvent, IIntegrationRequest<TResponse>
        where TResponse : class, IIntegrationResponse
    {
        public Guid RequestId { get; init; } = Guid.NewGuid();
    }

    /// <summary>
    /// 响应事件基类。
    /// </summary>
    /// <param name="RequestId">对应请求的关联 ID。</param>
    public abstract record IntegrationResponse(Guid RequestId) : IntegrationEvent, IIntegrationResponse;
}
