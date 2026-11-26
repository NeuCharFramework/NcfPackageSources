/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CloudflareProtectMiddlewareExtensions.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.39.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    /// Provides registration helpers for the Cloudflare protection middleware.
    /// </summary>
    public static class CloudflareProtectMiddlewareExtensions
    {
        /// <summary>
        /// Adds <see cref="CloudflareProtectMiddleware"/> to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder receiving the middleware.</param>
        /// <returns>The same application builder for fluent pipeline configuration.</returns>
        public static IApplicationBuilder UseCloudflareProtect(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CloudflareProtectMiddleware>();
        }
    }
}
