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
