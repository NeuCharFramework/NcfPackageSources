/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SmsExtensionResource.cs
    File description: SmsExtensionResource related implementation

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.12.14 integrate SmsExtension Register display metadata with shared resource localization

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SmsExtension;

public sealed class SmsExtensionResource
{
    public static string Get(string key, string? fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(SmsExtensionResource), key, fallback);
    }

    public static string Format(string key, string fallback, params object[] arguments)
    {
        return ResourceStringLocalizer.Format(typeof(SmsExtensionResource), key, fallback, arguments);
    }
}
