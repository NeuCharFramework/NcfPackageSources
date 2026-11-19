/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SwaggerResource.cs
    File description: SwaggerResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.19.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.Swagger
{
    /// <summary>
    /// Localization catalog owned and packaged by the Swagger module.
    /// </summary>
    public sealed class SwaggerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SwaggerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SwaggerResource), key, fallback, arguments);
        }
    }
}
