/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxDocsLinks.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// Provides localized documentation links for Sandbox environment setup.
/// </summary>
public static class SandboxDocsLinks
{
    public const string SiteRoot = "https://doc.ncf.pub";

    /// <summary>Chinese Sandbox environment setup documentation URL.</summary>
    public const string EnvironmentSetupZh = SiteRoot + "/zh/NcfPackageSources/xncf/sandbox-environment.html";

    /// <summary>
    /// English: Sandbox environment setup
    /// </summary>
    public const string EnvironmentSetupEn = SiteRoot + "/NcfPackageSources/xncf/sandbox-environment.html";

    /// <summary>
    /// Selects the environment setup documentation URL from a culture name.
    /// Chinese cultures use the localized page; all other cultures use the English page.
    /// </summary>
    /// <param name="cultureName">The optional culture name.</param>
    /// <returns>The localized setup documentation URL.</returns>
    public static string GetEnvironmentSetupUrl(string? cultureName = null)
    {
        if (!string.IsNullOrWhiteSpace(cultureName)
            && cultureName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return EnvironmentSetupZh;
        }

        //Legacy comment translated to English.
        if (!string.IsNullOrWhiteSpace(cultureName)
            && cultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return EnvironmentSetupEn;
        }

        return EnvironmentSetupZh;
    }
}
