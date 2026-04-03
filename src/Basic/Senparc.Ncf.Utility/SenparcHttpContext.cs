using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Microsoft.AspNetCore.Http
{
    //Reference: https://www.cnblogs.com/yuangang/archive/2016/08/08/5743660.html

    public static class SenparcHttpContext
    {
        /// <summary>
        /// Directory separator: "\" under Windows, "\" under Mac OS and Linux
        /// </summary>
        public static string DirectorySeparatorChar { get; } = Path.DirectorySeparatorChar.ToString();

        /// <summary>
        /// Absolute path to the directory containing the referencing program
        /// </summary>
        public static string ContentRootPath { get; } = DI.ServiceProvider.GetRequiredService<IHostingEnvironment>().ContentRootPath;

        /// <summary>
        /// Absolute path to the directory containing the referencing program
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
        /// Get the absolute path of the file
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns></returns>
        public static string MapPath(string path)
        {
            return IsAbsolute(path)
                ? path
                : Path.Combine(ContentRootPath, path.TrimStart('~', '/').Replace("/", DirectorySeparatorChar));
        }

        /// <summary>
        /// Get the absolute path of the file
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns></returns>
        public static string MapWebPath(string path)
        {
            return IsAbsolute(path)
                ? path
                : Path.Combine(ContentWebRootPath, path.TrimStart('~', '/').Replace("/", DirectorySeparatorChar));
        }

        /// <summary>
        /// Is it an absolute path?
        /// Determine whether the path contains ":" under windows
        /// Determine whether the path contains "\" under Mac OS and Linux
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns></returns>
        public static bool IsAbsolute(string path)
        {
            return Path.VolumeSeparatorChar == ':' ? path.IndexOf(Path.VolumeSeparatorChar) > 0 : path.IndexOf('\\') > 0;
        }
    }
}
