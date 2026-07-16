#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Dapr
{
    /// <summary>
    /// Localization catalog owned and packaged by the Dapr module.
    /// </summary>
    public sealed class DaprResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(DaprResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(DaprResource), key, fallback, arguments);
        }
    }
}
