#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.WeixinManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the WeixinManager module.
    /// </summary>
    public sealed class WeixinManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(WeixinManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(WeixinManagerResource), key, fallback, arguments);
        }
    }
}
