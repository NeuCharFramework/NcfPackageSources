/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenMapicUtility.cs
    文件功能描述：SenMapicUtility 相关实现
    
    
    创建标识：Senparc - 20250113
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    public static class SenMapicUtility
    {
        public static Stream GetReceiveStream(string contentEncoding, HttpResponseMessage response)
        {
            var responseStream = response.Content.ReadAsStream();
            
            if (contentEncoding?.ToLower() == "gzip")
            {
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return responseStream;
            }
        }
    }
}
