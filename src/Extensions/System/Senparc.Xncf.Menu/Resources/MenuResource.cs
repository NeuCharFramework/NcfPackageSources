#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Menu
{
    /// <summary>
    /// Localization catalog owned and packaged by the Menu module.
    /// </summary>
    public sealed class MenuResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(MenuResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(MenuResource), key, fallback, arguments);
        }
    }
}
