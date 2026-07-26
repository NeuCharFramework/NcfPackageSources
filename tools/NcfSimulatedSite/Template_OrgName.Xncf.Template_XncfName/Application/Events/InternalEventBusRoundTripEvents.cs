/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：InternalEventBusRoundTripEvents.cs
    文件功能描述：模板内部 EventBus 请求-响应回环事件


    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v1.1.0 补充示例模板 EventBus 请求-响应回环与多语言能力

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;
using System;

namespace Template_OrgName.Xncf.Template_XncfName.Application.Events
{
    /// <summary>
    /// 仅用于验证当前 XNCF 内部 EventBus 请求-响应链路，不携带用户、配置或环境数据。
    /// </summary>
    public sealed record InternalEventBusRoundTripRequest(DateTime SentAtUtc)
        : IntegrationRequest<InternalEventBusRoundTripResponse>;

    /// <summary>
    /// 内部回环响应，仅返回事件关联与处理时间信息。
    /// </summary>
    public sealed record InternalEventBusRoundTripResponse(
        Guid RequestId,
        DateTime SentAtUtc,
        DateTime HandledAtUtc)
        : IntegrationResponse(RequestId);
}
