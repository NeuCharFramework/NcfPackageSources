/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: CloudflareProtectMiddlewareExtensions.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.39.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;

namespace Senparc.Web.Infrastructure.Security
{
    public static class CloudflareProtectMiddlewareExtensions
    {
        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public static IApplicationBuilder UseCloudflareProtect(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CloudflareProtectMiddleware>();
        }
    }
}
