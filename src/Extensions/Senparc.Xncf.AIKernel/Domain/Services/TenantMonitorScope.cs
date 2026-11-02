/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：TenantMonitorScope.cs
    文件功能描述：TenantMonitorScope 相关定义与实现


    创建标识：Senparc - 20261001

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 完善微调任务、数据集、产物文件引用、导入导出和验证流程

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.MultiTenant;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    internal static class TenantMonitorScope
    {
        public static int GetTenantId(IServiceProvider serviceProvider)
        {
            return SiteConfig.SenparcCoreSetting.EnableMultiTenant
                ? MultiTenantHelper.TryGetAndCheckRequestTenantInfo(serviceProvider, nameof(TenantMonitorScope)).Id
                : 0;
        }
    }
}
