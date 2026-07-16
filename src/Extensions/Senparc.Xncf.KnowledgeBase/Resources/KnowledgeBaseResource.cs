#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.KnowledgeBase
{
    /// <summary>
    /// Localization catalog owned and packaged by the KnowledgeBase module.
    /// </summary>
    public sealed class KnowledgeBaseResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(KnowledgeBaseResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(KnowledgeBaseResource), key, fallback, arguments);
        }
    }
}
