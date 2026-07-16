#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Tenant
{
    /// <summary>
    /// Localization catalog owned and packaged by the Tenant module.
    /// </summary>
    public sealed class TenantResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(TenantResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(TenantResource), key, fallback, arguments);
        }
    }
}
