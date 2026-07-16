#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SystemPermission
{
    /// <summary>
    /// Localization catalog owned and packaged by the SystemPermission module.
    /// </summary>
    public sealed class SystemPermissionResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SystemPermissionResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SystemPermissionResource), key, fallback, arguments);
        }
    }
}
