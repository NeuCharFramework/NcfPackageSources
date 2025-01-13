using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using System.IO;
using System.Net;


namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    public static class SenMapicUtility
    {
        public static Stream GetReceiveStream(string contentEncoding, HttpWebResponse webResponse)
        {
            if (contentEncoding == "gzip")
            {
                //receiveStream = webResponse.GetResponseStream();
                return new GZipInputStream(webResponse.GetResponseStream());
                //receiveStream = new GZipStream(webResponse.GetResponseStream(), CompressionMode.Decompress);
            }
            else
            {
                return webResponse.GetResponseStream();
            }
        }
    }
}
