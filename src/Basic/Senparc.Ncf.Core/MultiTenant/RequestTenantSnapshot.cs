/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：RequestTenantSnapshot.cs
    文件功能描述：RequestTenantInfo 租户上下文快照，用于在后台任务与独立作用域中还原请求租户
    
    创建标识：Senparc - 20261028
    修改描述：vNext 新增请求租户快照，统一后台任务/隔离作用域的租户上下文传播

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Config;
using System;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// 当前请求租户上下文（RequestTenantInfo）的不可变快照。
    /// <para>
    /// RequestTenantInfo 为 Scoped 注册，请求作用域之外的后台任务/独立作用域（ChatGroup 运行、
    /// A2A 发布端点、单例状态存储等）在新建作用域时拿不到原请求的租户上下文，导致多租户模式下
    /// 租户过滤查询 fail-closed 或 TenantId 自动写入错误。使用本快照可以在同步前缀中捕获租户，
    /// 并在每个新建作用域中还原，使数据读写保持在发起请求所属租户内。
    /// </para>
    /// </summary>
    public sealed class RequestTenantSnapshot
    {
        public int Id { get; }
        public string Name { get; }
        public string TenantKey { get; }
        /// <summary>
        /// 捕获时是否匹配成功（false 表示快照不可用于还原）。
        /// </summary>
        public bool MatchSuccess { get; }

        private RequestTenantSnapshot(int id, string name, string tenantKey, bool matchSuccess)
        {
            Id = id;
            Name = name;
            TenantKey = tenantKey;
            MatchSuccess = matchSuccess;
        }

        /// <summary>
        /// 未匹配的空快照。
        /// </summary>
        public static RequestTenantSnapshot Unmatched { get; } = new(0, null, null, false);

        /// <summary>
        /// 从指定作用域捕获当前 RequestTenantInfo。
        /// <para>在请求作用域外捕获、作用域已释放或未匹配到租户时，返回 <see cref="Unmatched"/>（不抛异常）。</para>
        /// </summary>
        /// <param name="serviceProvider">发起请求所在作用域的 IServiceProvider</param>
        public static RequestTenantSnapshot Capture(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                return Unmatched;
            }

            RequestTenantInfo requestTenantInfo;
            try
            {
                requestTenantInfo = serviceProvider.GetService<RequestTenantInfo>();
            }
            catch (ObjectDisposedException)
            {
                return Unmatched;
            }

            if (requestTenantInfo == null || !requestTenantInfo.MatchSuccess)
            {
                return Unmatched;
            }

            return new RequestTenantSnapshot(
                requestTenantInfo.Id,
                requestTenantInfo.Name,
                requestTenantInfo.TenantKey,
                requestTenantInfo.MatchSuccess);
        }

        /// <summary>
        /// 将快照中的租户上下文还原到目标（新）作用域。
        /// <para>
        /// 仅在多租户启用且快照匹配成功时生效；还原后目标作用域的租户过滤查询与
        /// TenantId 自动写入均保持在快照所属租户内。快照未匹配时不做任何修改（保持 fail-closed）。
        /// </para>
        /// </summary>
        /// <param name="scopeServiceProvider">需要还原租户上下文的新作用域 IServiceProvider</param>
        public void Restore(IServiceProvider scopeServiceProvider)
        {
            if (scopeServiceProvider == null
                || !SiteConfig.SenparcCoreSetting.EnableMultiTenant
                || !MatchSuccess)
            {
                return;
            }

            var requestTenantInfo = scopeServiceProvider.GetRequiredService<RequestTenantInfo>();
            requestTenantInfo.Id = Id;
            requestTenantInfo.Name = Name;
            requestTenantInfo.TenantKey = TenantKey;
            requestTenantInfo.TryMatch(true);
        }
    }
}
