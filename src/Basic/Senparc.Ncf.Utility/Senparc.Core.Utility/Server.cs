/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Server.cs
    文件功能描述：Server 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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