#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Template_OrgName.Xncf.Template_XncfName
{
    /// <summary>
    /// Localization catalog owned and packaged by the TemplateSimulated module.
    /// </summary>
    public sealed class Template_XncfNameResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(Template_XncfNameResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(Template_XncfNameResource), key, fallback, arguments);
        }
    }
}
