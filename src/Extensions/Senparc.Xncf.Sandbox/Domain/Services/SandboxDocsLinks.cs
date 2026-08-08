/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxDocsLinks.cs
    文件功能描述：指向官方文档站点的稳定链接（镜像版本等细节以 Docs 为准）

    创建标识：Senparc - 20260808

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// 文档链接集中在此，避免在多个页面硬编码。
/// 镜像 tag、安装命令等易变内容请维护在 NcfDocs，勿在 UI 中写死。
/// </summary>
public static class SandboxDocsLinks
{
    public const string SiteRoot = "https://doc.ncf.pub";

    /// <summary>
    /// 中文：Sandbox 环境准备指南
    /// </summary>
    public const string EnvironmentSetupZh = SiteRoot + "/zh/NcfPackageSources/xncf/sandbox-environment.html";

    /// <summary>
    /// English: Sandbox environment setup
    /// </summary>
    public const string EnvironmentSetupEn = SiteRoot + "/NcfPackageSources/xncf/sandbox-environment.html";

    public static string GetEnvironmentSetupUrl(string? cultureName = null)
    {
        if (!string.IsNullOrWhiteSpace(cultureName)
            && cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return EnvironmentSetupZh;
        }

        // 默认中文文档（国内用户为主）；英文文化返回英文页
        if (!string.IsNullOrWhiteSpace(cultureName)
            && cultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return EnvironmentSetupEn;
        }

        return EnvironmentSetupZh;
    }
}
