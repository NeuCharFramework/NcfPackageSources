/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：INeuBellProvider.cs
    文件功能描述：纽铃模块监控公共契约


    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.4.0-preview3 新增运行时同步提供程序抽象

    修改标识：Senparc - 20260804
    修改描述：v0.4.0-preview3 将公共契约统一更名为 NeuBell/纽铃

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview5 扩展工作流与智能体共享契约，支持 NeuBell 通知和对象编辑元数据

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.NeuBell
{
    /// <summary>
    /// 当前请求的最小上下文。模块不应依赖 Admin 的实体、DbContext 或页面类型。
    /// </summary>
    public sealed record NeuBellRequestContext(string UserId, string TenantId = null);

    /// <summary>
    /// Footer 中可显示的一项模块状态。
    /// </summary>
    public sealed record NeuBellItem(
        string Id,
        string Title,
        string Summary,
        int Count,
        string Severity,
        string DetailUrl,
        DateTimeOffset UpdatedAt);

    /// <summary>
    /// 一个 XNCF 模块提供的纽铃快照。
    /// </summary>
    public sealed record NeuBellSnapshot(
        string ProviderId,
        string ModuleUid,
        string DisplayName,
        string Icon,
        bool DefaultVisible,
        IReadOnlyList<NeuBellItem> Items);

    /// <summary>
    /// XNCF 通过实现并注册此接口向 Admin Footer 提供监控信息。
    /// </summary>
    public interface INeuBellProvider
    {
        string ProviderId { get; }

        /// <summary>
        /// 提供此状态的 XNCF 模块 UID。使用默认接口实现，避免新增成员破坏
        /// 已编译的旧 Provider；未声明 UID 的 Provider 会被 Admin 聚合器安全忽略。
        /// </summary>
        string ModuleUid => null;

        ValueTask<NeuBellSnapshot> GetSnapshotAsync(
            NeuBellRequestContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 可选的纽铃消费能力。Provider 不实现此接口时，调用方只能导航至其 DetailUrl，
    /// 不会把“点击查看”错误地当成业务已处理（例如 DesktopBridge 的设备审核）。
    /// </summary>
    public interface INeuBellConsumableProvider
    {
        /// <summary>只消费当前订阅下指定的一条提醒；返回实际消费数量。</summary>
        ValueTask<int> ConsumeItemAsync(
            NeuBellRequestContext context,
            string itemId,
            CancellationToken cancellationToken = default);

        /// <summary>消费当前订阅下当前用户可见的全部提醒；返回实际消费数量。</summary>
        ValueTask<int> ConsumeAllAsync(
            NeuBellRequestContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 模块在状态变化后通知 Admin Host 刷新对应 Provider；通知不得携带敏感业务正文。
    /// </summary>
    public interface INeuBellPublisher
    {
        ValueTask NotifyChangedAsync(string providerId, CancellationToken cancellationToken = default);
    }
}
