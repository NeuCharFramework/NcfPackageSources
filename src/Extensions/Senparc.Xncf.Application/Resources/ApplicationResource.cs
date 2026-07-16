#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Application
{
    /// <summary>
    /// Localization catalog owned and packaged by the Application module.
    /// </summary>
    public sealed class ApplicationResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(ApplicationResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(ApplicationResource), key, fallback, arguments);
        }
    }
}
