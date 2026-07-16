#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// Localization catalog owned and packaged by the PromptRange module.
    /// </summary>
    public sealed class PromptRangeResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(PromptRangeResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(PromptRangeResource), key, fallback, arguments);
        }
    }
}
