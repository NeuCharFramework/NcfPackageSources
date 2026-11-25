/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Server.cs
    File description: Server related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;

namespace Senparc.Ncf.Core.Utility
{
    public static class Server
    {
        private static string _appDomainAppPath;

        public static string AppDomainAppPath
        {
            get
            {
                if (_appDomainAppPath == null)
                {
                    _appDomainAppPath = SenparcHttpContext.ContentRootPath;
                }
                return _appDomainAppPath;
            }
            set
            {
                _appDomainAppPath = value;
            }
        }

        /// <summary>
        /// Maps an application-relative virtual path to a physical path.
        /// </summary>
        /// <param name="virtualPath">The virtual path to map.</param>
        /// <returns>The mapped path, or an empty string when <paramref name="virtualPath"/> is null.</returns>
        public static string GetMapPath(string virtualPath)
        {
            if (virtualPath == null)
            {
                return "";
            }

            return SenparcHttpContext.MapPath(virtualPath);
        }

        /// <summary>
        /// Maps an application-relative virtual path to its web-facing path representation.
        /// </summary>
        /// <param name="virtualPath">The virtual path to map.</param>
        /// <returns>The mapped web path, or an empty string when <paramref name="virtualPath"/> is null.</returns>
        public static string GetWebMapPath(string virtualPath)
        {
            if (virtualPath == null)
            {
                return "";
            }

            return SenparcHttpContext.MapWebPath(virtualPath);
        }

        public static HttpContext HttpContext
        {
            get
            {
                return SenparcHttpContext.Current;
            }
        }
    }
}
