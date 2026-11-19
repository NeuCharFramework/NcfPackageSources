/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ReloadPageResource.cs
    File description: ReloadPageResource related implementation

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.14 integrate ReloadPage Register display metadata with shared resource localization

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.ReloadPage;

public sealed class ReloadPageResource
{
    public static string Get(string key, string? fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(ReloadPageResource), key, fallback);
    }

    public static string Format(string key, string fallback, params object[] arguments)
    {
        return ResourceStringLocalizer.Format(typeof(ReloadPageResource), key, fallback, arguments);
    }
}
