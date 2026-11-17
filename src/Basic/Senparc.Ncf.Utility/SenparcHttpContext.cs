/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcHttpContext.cs
    File description: SenparcHttpContext related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Microsoft.AspNetCore.Http
{
    //Legacy comment translated to English.

    public static class SenparcHttpContext
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string DirectorySeparatorChar { get; } = Path.DirectorySeparatorChar.ToString();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string ContentRootPath { get; } = DI.ServiceProvider.GetRequiredService<IHostingEnvironment>().ContentRootPath;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string ContentWebRootPath { get; } = DI.ServiceProvider.GetRequiredService<IHostingEnvironment>().WebRootPath;


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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="path">filepath</param>
        /// <returns></returns>
        public static string MapPath(string path)
        {
            return IsAbsolute(path)
                ? path
                : Path.Combine(ContentRootPath, path.TrimStart('~', '/').Replace("/", DirectorySeparatorChar));
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="path">filepath</param>
        /// <returns></returns>
        public static string MapWebPath(string path)
        {
            return IsAbsolute(path)
                ? path
                : Path.Combine(ContentWebRootPath, path.TrimStart('~', '/').Replace("/", DirectorySeparatorChar));
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="path">path</param>
        /// <returns></returns>
        public static bool IsAbsolute(string path)
        {
            return Path.VolumeSeparatorChar == ':' ? path.IndexOf(Path.VolumeSeparatorChar) > 0 : path.IndexOf('\\') > 0;
        }
    }
}
