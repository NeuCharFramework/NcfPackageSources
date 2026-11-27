/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SwaggerResource.cs
    File description: SwaggerResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.19.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.20.12.2 Improve source documentation, headers, and module metadata consistency.

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
        /// <summary>
        /// Resolves a localized Swagger resource value.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="fallback">The fallback value.</param>
        /// <returns>The localized or fallback value.</returns>
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(SwaggerResource), key, fallback);
        }

        /// <summary>
        /// Resolves and formats a localized Swagger resource value.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="fallback">The fallback format string.</param>
        /// <param name="arguments">The format arguments.</param>
        /// <returns>The formatted localized or fallback value.</returns>
        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(SwaggerResource), key, fallback, arguments);
        }
    }
}
