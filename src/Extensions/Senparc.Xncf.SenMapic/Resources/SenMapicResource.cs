#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.SenMapic
{
    /// <summary>
    /// Localization catalog owned and packaged by the SenMapic module.
    /// </summary>
    public sealed class SenMapicResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SenMapicResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SenMapicResource), key, fallback, arguments);
        }
    }
}
