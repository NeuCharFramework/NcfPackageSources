/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicUtility.cs
    File description: SenMapicUtility related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

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
