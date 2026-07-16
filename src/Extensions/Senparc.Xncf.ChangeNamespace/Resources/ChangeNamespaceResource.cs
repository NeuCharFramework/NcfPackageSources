#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.ChangeNamespace
{
    /// <summary>
    /// Localization catalog owned and packaged by the ChangeNamespace module.
    /// </summary>
    public sealed class ChangeNamespaceResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(ChangeNamespaceResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(ChangeNamespaceResource), key, fallback, arguments);
        }
    }
}
