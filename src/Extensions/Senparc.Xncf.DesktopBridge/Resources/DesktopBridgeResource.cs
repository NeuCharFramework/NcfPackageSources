#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.DesktopBridge;

public sealed class DesktopBridgeResource
{
    public static string Get(string key, string? fallback = null)
    {
        return ResourceStringLocalizer.Get(typeof(DesktopBridgeResource), key, fallback);
    }

    public static string Format(string key, string fallback, params object[] arguments)
    {
        return ResourceStringLocalizer.Format(typeof(DesktopBridgeResource), key, fallback, arguments);
    }
}
