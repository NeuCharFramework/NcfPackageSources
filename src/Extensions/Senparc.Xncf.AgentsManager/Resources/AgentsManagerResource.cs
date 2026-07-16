#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AgentsManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the AgentsManager module.
    /// </summary>
    public sealed class AgentsManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AgentsManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AgentsManagerResource), key, fallback, arguments);
        }
    }
}
