/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicUtility.cs
    File description: SenMapicUtility related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

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
        /// <summary>
        /// Returns a response stream, transparently decompressing gzip content.
        /// </summary>
        /// <param name="contentEncoding">The response content-encoding value.</param>
        /// <param name="response">The HTTP response containing the body stream.</param>
        /// <returns>The decompressed or original response stream.</returns>
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
