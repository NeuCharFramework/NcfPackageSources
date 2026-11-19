/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Template_XncfNameResource.cs
    File description: Template_XncfNameResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v1.1.0 update feature parameters and page localization for the sample XNCF module

    Modified by: Senparc - 20260724
    Change description: v1.1.0 improve multilingual support for XNCF template pages and resources

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
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
