#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.XncfModuleManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the XncfModuleManager module.
    /// </summary>
    public sealed class XncfModuleManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(XncfModuleManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(XncfModuleManagerResource), key, fallback, arguments);
        }
    }
}
