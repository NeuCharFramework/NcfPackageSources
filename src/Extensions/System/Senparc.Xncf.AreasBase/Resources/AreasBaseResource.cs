#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AreasBase
{
    /// <summary>
    /// Localization catalog owned and packaged by the AreasBase module.
    /// </summary>
    public sealed class AreasBaseResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AreasBaseResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AreasBaseResource), key, fallback, arguments);
        }
    }
}
