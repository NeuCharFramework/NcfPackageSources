/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EmailExtensionResource.cs
    File description: EmailExtensionResource related implementation

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.14 integrate EmailExtension Register display metadata with shared resource localization

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.EmailExtension;

public sealed class EmailExtensionResource
{
    public static string Get(string key, string? fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(EmailExtensionResource), key, fallback);
    }

    public static string Format(string key, string fallback, params object[] arguments)
    {
        return ResourceStringLocalizer.Format(typeof(EmailExtensionResource), key, fallback, arguments);
    }
}
