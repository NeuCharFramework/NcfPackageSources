/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IAuthorizedIntegrationSyncEvent.cs
    文件功能描述：需要按身份隔离的集成事件同步通知契约

    创建标识：Senparc - 20260726

    修改标识：Senparc - 20260726
    修改描述：v0.3.0-preview2 同步模块功能与兼容性改进

----------------------------------------------------------------*/

namespace Senparc.Ncf.Shared.Abstractions.Events
{
    /// <summary>
    /// 可安全同步到受保护外部客户端的事件通知。
    /// </summary>
    /// <remarks>
    /// 实现只应携带资源标识和变更类型，不应包含密码、令牌、消息正文等敏感数据。
    /// 消费端必须同时验证 <see cref="OwnerId"/> 与 <see cref="RequiredPolicy"/>。
    /// </remarks>
    public interface IAuthorizedIntegrationSyncEvent : IIntegrationEvent
    {
        /// <summary>
        /// 同步频道，例如 <c>admin-chat</c>。
        /// </summary>
        string Channel { get; }

        /// <summary>
        /// 资源所有者的稳定标识，用于隔离不同登录账号。
        /// </summary>
        string OwnerId { get; }

        /// <summary>
        /// 发生变化的资源标识。
        /// </summary>
        string ResourceId { get; }

        /// <summary>
        /// 不含业务数据的变更类型，例如 <c>created</c> 或 <c>messages-changed</c>。
        /// </summary>
        string Action { get; }

        /// <summary>
        /// 外部同步端点必须满足的授权策略。
        /// </summary>
        string RequiredPolicy { get; }
    }
}
