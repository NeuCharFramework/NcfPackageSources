/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Server.cs

    File description: Server related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

        public static string GetMapPath(string virtualPath)
        {
            if (virtualPath == null)
            {
                return "";
            }

            return SenparcHttpContext.MapPath(virtualPath);
        }

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