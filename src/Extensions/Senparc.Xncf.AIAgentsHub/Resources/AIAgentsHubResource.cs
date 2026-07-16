#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.AIAgentsHub
{
    /// <summary>
    /// Localization catalog owned and packaged by the AIAgentsHub module.
    /// </summary>
    public sealed class AIAgentsHubResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(AIAgentsHubResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(AIAgentsHubResource), key, fallback, arguments);
        }
    }
}
