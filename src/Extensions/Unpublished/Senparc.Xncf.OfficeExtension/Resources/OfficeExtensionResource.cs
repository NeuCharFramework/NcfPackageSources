/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: OfficeExtensionResource.cs
    File description: OfficeExtensionResource related implementation

    Created by: Senparc - 20261111
    Modified by: Senparc - 20261111
    Change description: v0.12.14 integrate OfficeExtension Register display metadata with shared resource localization

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.OfficeExtension;

public sealed class OfficeExtensionResource
{
    public static string Get(string key, string? fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(OfficeExtensionResource), key, fallback);
    }

    public static string Format(string key, string fallback, params object[] arguments)
    {
        return ResourceStringLocalizer.Format(typeof(OfficeExtensionResource), key, fallback, arguments);
    }
}
