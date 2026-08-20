/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcfAuthorizationPolicyNames.cs
    文件功能描述：NCF 标准授权策略名称

    修改标识：Senparc - 20260729
    修改描述：v0.26.0-preview3 修复安装状态写入并统一授权策略名称

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    /// NCF 跨模块使用的授权策略名称。
    /// 策略的具体要求由宿主注册，名称由 Core 统一维护。
    /// </summary>
    public static class NcfAuthorizationPolicyNames
    {
        /// <summary>
        /// 已登录的后台管理员。
        /// </summary>
        public const string AdminOnly = "AdminOnly";

        /// <summary>
        /// AdminMember claim 的名称。
        /// </summary>
        public const string AdminMemberClaim = "AdminMember";
    }
}
