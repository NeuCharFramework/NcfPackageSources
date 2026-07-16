using Senparc.Ncf.Core.Localization;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Shared localization catalog for metadata owned by the official NCF/XNCF
    /// modules. Third-party modules should keep their resources in their own
    /// assembly and use <see cref="ResourceStringLocalizer"/> directly.
    /// </summary>
    public sealed class NcfBuiltInResource
    {
        public static string Get(string key, string fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(NcfBuiltInResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(NcfBuiltInResource), key, fallback, arguments);
        }
    }
}
