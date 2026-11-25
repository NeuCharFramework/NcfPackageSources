/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcHttpContext.cs
    File description: SenparcHttpContext related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides process-wide access to common ASP.NET Core hosting paths and the current HTTP context.
    /// </summary>
    public static class SenparcHttpContext
    {
        /// <summary>
        /// Gets the platform-specific directory separator as a string.
        /// </summary>
        public static string DirectorySeparatorChar { get; } = Path.DirectorySeparatorChar.ToString();

        /// <summary>
        /// Gets the host content-root path resolved from the registered hosting environment.
        /// </summary>
        public static string ContentRootPath { get; } = DI.ServiceProvider.GetRequiredService<IHostingEnvironment>().ContentRootPath;

        /// <summary>
        /// Gets the host web-root path resolved from the registered hosting environment.
        /// </summary>
        public static string ContentWebRootPath { get; } = DI.ServiceProvider.GetRequiredService<IHostingEnvironment>().WebRootPath;

        /// <summary>
        /// Gets the current HTTP context from the registered accessor.
        /// </summary>
        /// <returns>The current context, or <see langword="null"/> when no accessor or context is available.</returns>
        public static HttpContext Current
        {
            get
            {
                try
                {
                    object factory = DI.ServiceProvider.GetService(typeof(IHttpContextAccessor));
                    HttpContext context = ((HttpContextAccessor)factory).HttpContext;
                    return context;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Converts an application-relative or absolute path into a path under the host content root.
        /// Absolute paths are returned unchanged.
        /// </summary>
        /// <param name="path">The path to map, such as <c>~/App_Data/file.txt</c> or an absolute path.</param>
        /// <returns>The mapped content-root path.</returns>
        public static string MapPath(string path)
        {
            return IsAbsolute(path)
                ? path
                : Path.Combine(ContentRootPath, path.TrimStart('~', '/').Replace("/", DirectorySeparatorChar));
        }

        /// <summary>
        /// Converts an application-relative or absolute path into a path under the host web root.
        /// Absolute paths are returned unchanged.
        /// </summary>
        /// <param name="path">The path to map, such as <c>~/css/site.css</c> or an absolute path.</param>
        /// <returns>The mapped web-root path.</returns>
        public static string MapWebPath(string path)
        {
            return IsAbsolute(path)
                ? path
                : Path.Combine(ContentWebRootPath, path.TrimStart('~', '/').Replace("/", DirectorySeparatorChar));
        }

        /// <summary>
        /// Determines whether a path uses the platform's absolute-path marker.
        /// </summary>
        /// <param name="path">The path to inspect.</param>
        /// <returns><see langword="true"/> when the path is recognized as absolute; otherwise, <see langword="false"/>.</returns>
        public static bool IsAbsolute(string path)
        {
            return Path.VolumeSeparatorChar == ':' ? path.IndexOf(Path.VolumeSeparatorChar) > 0 : path.IndexOf('\\') > 0;
        }
    }
}
