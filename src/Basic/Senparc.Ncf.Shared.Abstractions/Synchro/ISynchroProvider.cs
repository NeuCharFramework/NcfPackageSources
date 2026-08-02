/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ISynchroProvider.cs
    文件功能描述：Synchro（灵犀）模块监控公共契约

    创建标识：Senparc - 20260802
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Shared.Abstractions.Synchro
{
    /// <summary>
    /// 当前请求的最小上下文。模块不应依赖 Admin 的实体、DbContext 或页面类型。
    /// </summary>
    public sealed record SynchroRequestContext(string UserId, string TenantId = null);

    /// <summary>
    /// Footer 中可显示的一项模块状态。
    /// </summary>
    public sealed record SynchroItem(
        string Id,
        string Title,
        string Summary,
        int Count,
        string Severity,
        string DetailUrl,
        DateTimeOffset UpdatedAt);

    /// <summary>
    /// 一个 XNCF 模块提供的灵犀快照。
    /// </summary>
    public sealed record SynchroSnapshot(
        string ProviderId,
        string ModuleUid,
        string DisplayName,
        string Icon,
        bool DefaultVisible,
        IReadOnlyList<SynchroItem> Items);

    /// <summary>
    /// XNCF 通过实现并注册此接口向 Admin Footer 提供监控信息。
    /// </summary>
    public interface ISynchroProvider
    {
        string ProviderId { get; }

        /// <summary>
        /// 提供此状态的 XNCF 模块 UID。使用默认接口实现，避免新增成员破坏
        /// 已编译的旧 Provider；未声明 UID 的 Provider 会被 Admin 聚合器安全忽略。
        /// </summary>
        string ModuleUid => null;

        ValueTask<SynchroSnapshot> GetSnapshotAsync(
            SynchroRequestContext context,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 模块在状态变化后通知 Admin Host 刷新对应 Provider；通知不得携带敏感业务正文。
    /// </summary>
    public interface ISynchroPublisher
    {
        ValueTask NotifyChangedAsync(string providerId, CancellationToken cancellationToken = default);
    }
}
