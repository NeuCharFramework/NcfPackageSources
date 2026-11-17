/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SenMapicEngineExtension.cs

    File description: SenMapicEngineExtension related implementation

    

    

    Created by: Senparc - 20250113

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260729

    Change description: v0.3.1-preview3 Updated module behavior and related support.



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    using Senparc.CO2NET.Extensions;
    using Senparc.CO2NET.Trace;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class SenMapicEngine
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private void ResetUrlPath(string rootDomain)
        {
            _urlPath.Clear();
            _urlPath.Add(new UrlPathCollection(rootDomain, 0, new List<string>()));//Legacy comment translated to English.
            for (int i = 1; i <= _maxDeep; i++)
            {
                _urlPath.Add(new UrlPathCollection(null, i, new List<string>()));
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetFullDomain(string url)
        {
            var reg = Regex.Match(url, "(^http(s)?://[^/]+)", RegexOptions.IgnoreCase);
            if (reg.Success)
            {
                return reg.Value;
            }
            return null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetDomain(string url)
        {
            var reg = Regex.Match(url, "(?<=http(s)?://)([^/]+)", RegexOptions.IgnoreCase);
            if (reg.Success)
            {
                return reg.Value;
            }
            return null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetProtocol(string url)
        {
            var reg = Regex.Match(url.ToLower(), "(http(s)?)(?=://)");
            if (reg.Success)
            {
                return reg.Value;
            }
            return null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string RemoveUrlEndingSlash(string url)
        {
            if (url == null || url.EndsWith("../") || url == "/" || !url.EndsWith("/"))
            {
                return url;
            }
            else
            {
                while (url.EndsWith("/"))
                {
                    url = url.Substring(0, url.Length - 1);//Legacy comment translated to English.
                }
                return url;
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string RemoveUrlBeginingSlash(string url)
        {
            if (url == null || url == "/" || !url.StartsWith("/"))
            {
                return url;
            }
            else
            {
                while (url.StartsWith("/"))
                {
                    url = url.Substring(1, url.Length - 1);//Legacy comment translated to English.
                }
                return url;
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parentUrl"></param>
        /// <returns></returns>
        public string GetFullUrl(string url, string parentUrl)
        {
            string newUrl = null;
            try
            {
                newUrl = this.RemoveUrlEndingSlash(url);
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                if (newUrl.StartsWith("mailto:")
                        || newUrl.StartsWith("javascript:")
                        || newUrl.StartsWith("ftp://")
                        || newUrl.StartsWith("skype:")
                        || newUrl.StartsWith("callto:")//Legacy comment translated to English.
                        || newUrl.StartsWith("tencent:")
                        || newUrl.StartsWith("msnim:")
                        || newUrl.StartsWith("file:")
                        || newUrl.StartsWith("ymsgr:") //Legacy comment translated to English.
                        || newUrl.StartsWith("\\")
                        || newUrl.StartsWith("+")
                        || (!newUrl.Contains("?") && (this.EndWithAny(newUrl, new[] { ".js", ".mp3", ".chm", ".wmv", ".flv", ".zip", ".rar", ".jpg", ".gif", ".bmp", ".png" })))
                        //|| newUrl.EndsWith(".mp3")
                        //|| newUrl.EndsWith(".chm")
                        //|| newUrl.EndsWith(".wmv")
                        //|| newUrl.EndsWith(".flv")
                        //|| newUrl.EndsWith(".zip")
                        //|| newUrl.EndsWith(".rar")
                        )
                {
                    return null;
                }

                string tempFullUrl;

                if (Regex.IsMatch(newUrl.ToUpper(), "^HTTP(S){0,1}://", RegexOptions.IgnoreCase))//Legacy comment translated to English.
                {
                    if (!IsAvailableUrl(newUrl))
                    {
                        return null;//Legacy comment translated to English.
                    }

                    string newUrlDomain = GetDomain(newUrl);
                    if (newUrlDomain == null || !_currentDomain.ToUpper().Contains(newUrlDomain.ToUpper()))
                    {
                        return null;//Legacy comment translated to English.
                    }

                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //if (!topDomain.Contains("."))
                    //{
                    //Legacy comment translated to English.
                    //}
                    //Legacy comment translated to English.
                    //{
                    //Legacy comment translated to English.
                    //}

                    //if (!parentUrl.IsNullOrEmpty() && newUrlDomain.ToUpper() != _currentDomain.ToUpper())
                    //{
                    //Legacy comment translated to English.
                    //}

                    tempFullUrl = newUrl;
                }
                else
                {
                    tempFullUrl = parentUrl;//Legacy comment translated to English.

                    string parentFullDomain = GetFullDomain(parentUrl);//Legacy comment translated to English.

                    //Legacy comment translated to English.
                    if (newUrl.StartsWith("/"))
                    {
                        tempFullUrl = parentFullDomain + newUrl;//Legacy comment translated to English.
                    }
                    else if (newUrl.StartsWith("?"))//Legacy comment translated to English.
                    {
                        if (tempFullUrl.Contains("?"))
                        {
                            tempFullUrl = tempFullUrl.Substring(0, tempFullUrl.IndexOf("?")) + newUrl;
                        }
                        else
                        {
                            tempFullUrl += newUrl;
                        }
                    }
                    else//Legacy comment translated to English.
                    {
                        if (parentUrl.EndsWith("/"))//Legacy comment translated to English.
                        {
                            tempFullUrl += newUrl;
                        }
                        else
                        {
                            if (Regex.IsMatch(tempFullUrl.ToUpper(), @"^HTTP(S)?://", RegexOptions.IgnoreCase) && tempFullUrl.LastIndexOf("/") <= 7)
                            {
                                //Legacy comment translated to English.
                                tempFullUrl += "/";//Legacy comment translated to English.
                            }
                            tempFullUrl = tempFullUrl.Substring(0, tempFullUrl.LastIndexOf("/")) + "/" + newUrl;//Legacy comment translated to English.
                        }
                    }


                    if (tempFullUrl.Contains("/../"))//ornewUrl.Contains("../")
                    {
                        if (_currentPageCount > 0 && _currentUrl.ToUpper().StartsWith(newUrl.ToUpper()))
                        {
                            return null;//Legacy comment translated to English.
                        }

                        int domainLength = _currentOriginalFullDomain.Length;
                        while (tempFullUrl.Contains("/../"))
                        {
                            int indexOfParentMark = tempFullUrl.IndexOf("/../");//Legacy comment translated to English.
                            string parentUrlPath;
                            if (indexOfParentMark <= domainLength)//Legacy comment translated to English.
                            {
                                parentUrlPath = _currentOriginalFullDomain;
                            }
                            else
                            {
                                int indexOfParentLevel = tempFullUrl.Substring(0, indexOfParentMark).LastIndexOf("/");//Legacy comment translated to English.
                                parentUrlPath = tempFullUrl.Substring(0, indexOfParentLevel);//Legacy comment translated to English.
                            }

                            string afterPath = tempFullUrl.Substring(indexOfParentMark + 4, tempFullUrl.Length - indexOfParentMark - 4);//Legacy comment translated to English.
                            tempFullUrl = parentUrlPath + "/" + afterPath;
                        }

                        if (!tempFullUrl.ToUpper().StartsWith(_currentProtocol.ToUpper() + "://" + _currentDomain.ToUpper()))
                        {
                            return null;//Legacy comment translated to English.
                        }
                        newUrl = tempFullUrl;
                    }
                }

                return this.RemoveUrlEndingSlash(tempFullUrl);
            }
            catch (Exception e)
            {
                var ex = new Exception($"GetFullUrl方法内部出错：URL:{url},newUrl:{newUrl}", e);
                SenparcTrace.BaseExceptionLog(ex);
                throw;
            }
        }



        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<UrlData> CrawSingleUrl(string url)
        {
            if (string.IsNullOrEmpty(_currentDomain))
            {
                _currentDomain = this.GetDomain(url);
            }
            return await CrawlUrl(url, null, 0, true, null);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        ///Legacy comment translated to English.
        private async Task<UrlData> CrawlUrl(string url, string parentUrl, int deep, bool singleUrl, string linkText)
        {
            //Legacy comment translated to English.

            UrlData urlData = null;
            HttpResponseMessage webResponse = null;
            DateTime requestStartTime = DateTime.MinValue;
            DateTime requestEndTime = DateTime.MinValue;
            try
            {
                //Legacy comment translated to English.
                //if (deep > _maxDeep || _currentUrls.Count >= _maxPageCount)
                //{
                //Legacy comment translated to English.
                //}

                url = RemoveUrlEndingSlash(url);//Legacy comment translated to English.

                if (!this.IsAvailableUrl(url)//Legacy comment translated to English.
                    || this.ContainsFilterOmitWord(url)//Legacy comment translated to English.
                    || this._currentUrls.ContainsKey(url) //Legacy comment translated to English.
                    )
                {
                    return null;
                }

                if (!this._currentAvaliableUrlTemp.ContainsKey(url))
                {
                    this._currentAvaliableUrlTemp.Add(url, new AvailableUrl(url, parentUrl, deep, linkText, AvailableUrlStatus.Started));//Legacy comment translated to English.
                }
                else if (this._currentAvaliableUrlTemp[url].Status != AvailableUrlStatus.UnStart)
                {
                    return null;//Legacy comment translated to English.
                }

                //Legacy comment translated to English.
                var responseResult = await RequestPage(url);//Legacy comment translated to English.
                webResponse = responseResult.response;
                requestStartTime = responseResult.requestStartTime;
                requestEndTime = responseResult.requestEndTime;

                var finalRequestUri = webResponse?.RequestMessage?.RequestUri;
                if (finalRequestUri == null || !this.IsAvailableUrl(finalRequestUri.AbsoluteUri))
                {
                    return null;
                }

                this._currentAvaliableUrlTemp[url].Status = AvailableUrlStatus.Finished;//Legacy comment translated to English.

                //Legacy comment translated to English.
                if (url.ToUpper() != webResponse.RequestMessage.RequestUri.AbsoluteUri.ToUpper())
                {
                    //Legacy comment translated to English.
                    this._currentAvaliableUrlTemp[url].Url = webResponse.RequestMessage.RequestUri.AbsoluteUri;//Legacy comment translated to English.
                    url = webResponse.RequestMessage.RequestUri.AbsoluteUri;//Legacy comment translated to English.
                    if (this._currentUrls.ContainsKey(url) || this.ContainsFilterOmitWord(url))
                    {
                        return null;//Legacy comment translated to English.
                    }
                    else if (!url.IsNullOrEmpty() && !_currentDomain.ToUpper().Contains(this.GetDomain(url).ToUpper()))
                    {
                        return null;//Legacy comment translated to English.
                    }
                }

                if (webResponse.StatusCode != HttpStatusCode.OK)
                {
                    return new UrlData(url, _currentDeep, "", "", (int)webResponse.StatusCode, webResponse.Content.Headers.ContentLength ?? 0, null, (int)TimeSpan.FromTicks(_requestPageTicks).TotalMilliseconds, linkText);
                }
                else if (!webResponse.Content.Headers.ContentType?.MediaType?.StartsWith("text") ?? true)
                {
                    return new UrlData(url, _currentDeep, "", "", (int)webResponse.StatusCode, webResponse.Content.Headers.ContentLength ?? 0, null, linkText);
                }

                //DateTime dt1 = DateTime.Now;

                //Legacy comment translated to English.
                string ce = webResponse.Content.Headers.ContentEncoding.FirstOrDefault();
                Stream receiveStream = null;
                receiveStream = SenMapicUtility.GetReceiveStream(ce, webResponse);

                //Legacy comment translated to English.
                string contentType = webResponse.Content.Headers.ContentType?.MediaType;
                string charterSet = webResponse.Content.Headers.ContentType?.CharSet;

                //Legacy comment translated to English.
                charterSet = GetCharterSetFromContentType(charterSet);

                if (!contentType.ToUpper().StartsWith("TEXT/"))
                {
                    return null;
                }

                Encoding encode = GetEncodingTypeFormCharterSet(charterSet, true);
                string htmlTotal = GetTrueEncodingHtml(receiveStream, ref encode) ?? ""; //Legacy comment translated to English.
                receiveStream.Dispose();

                int htmlByteCount = htmlTotal.IsNullOrEmpty() ? 0 : encode.GetByteCount(htmlTotal); //System.Text.Encoding.Default.GetByteCount(htmlTotal);
                string titleHtml = htmlTotal.Length > 1000 ? htmlTotal.Substring(0, 1000) : htmlTotal;//Legacy comment translated to English.
                urlData = new UrlData(url, deep, htmlTotal, titleHtml, (int)HttpStatusCode.OK, (double)htmlByteCount / 1024, null, (int)TimeSpan.FromTicks(_requestPageTicks).TotalMilliseconds, linkText);

                #region Legacy section
                //DateTime dt2 = DateTime.Now;
                //Legacy comment translated to English.
                //    (dt2 - dt1).TotalMilliseconds.ToString("###,###ms"),
                //    webResponse.ContentLength,
                //    ((dt2 - dt1).TotalMilliseconds / ((webResponse.ContentLength == 0 ? -1 : webResponse.ContentLength) / 1000)).ToString("###,###ms")));

                //if (_currentDeep == _maxDeep || _currentUrls.Count == _maxPageCount)
                //{
                //Legacy comment translated to English.
                //}
                #endregion
            }
            catch (WebException e)
            {
                int result = 0;
                if (e.Response is HttpWebResponse)
                {
                    result = (int)((HttpWebResponse)e.Response).StatusCode;
                }
                else //Legacy comment translated to English.
                {
                    result = 9;

                    //Legacy comment translated to English.
                    if (this._currentUrls.Count(z => z.Value.Result == 9) >= this._maxTimeoutTimes)
                    {
                        this.StopBuild();
                    }
                }
                //Legacy comment translated to English.
                return new UrlData(url, deep, "", "", result, 0, null, linkText);
            }
            catch (Exception e)
            {
                var ex = new Exception($"SitemapDebug异常({url})", e);
                //Legacy comment translated to English.
                SenparcTrace.BaseExceptionLog(ex);
                return new UrlData(url, deep, "", "", 0, 0, null, linkText);
            }
            finally
            {
                if (webResponse != null)
                {
                    webResponse.Dispose();//Legacy comment translated to English.
                }

                if (urlData != null)
                {
                    TotalPageSizeKB += urlData.SizeKB;

                    int currentRequestMilliSeconds = (int)(requestEndTime - requestStartTime).TotalMilliseconds;
                    _requestPageCount++;
                    _requestPageTicks += (requestEndTime - requestStartTime).Ticks;//Legacy comment translated to English.

                    urlData.ResponseMillionSeconds = currentRequestMilliSeconds;
                }

                if (!singleUrl && (DateTime.Now - this.CurrentSiteStartTime).TotalMinutes >= MaxBuildMinutesForSingleSite)
                {
                    this.StopBuild();
                    SenparcTrace.SendCustomLog("SiteMap", $"SitemapBuild超时退出（{MaxBuildMinutesForSingleSite}分钟）。当前Url:{url}");
                }
            }
            return urlData;//Legacy comment translated to English.
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="charterSet"></param>
        /// <returns></returns>
        private static string GetCharterSetFromContentType(string charterSet)
        {
            if (charterSet != null)
            {
                int splitIndex = charterSet.IndexOf(';');
                if (splitIndex != -1 && splitIndex < charterSet.Length - 1)
                {
                    charterSet = charterSet.Substring(splitIndex + 1, charterSet.Length - splitIndex - 1).Trim();
                    int equalIndex = charterSet.IndexOf('=');
                    if (equalIndex != -1 && equalIndex < charterSet.Length - 1)
                    {
                        charterSet = charterSet.Substring(equalIndex + 1, charterSet.Length - equalIndex - 1).Trim();
                    }
                    else
                    {
                        charterSet = null;
                    }
                }
            }
            return charterSet;
        }

        /// <summary>
        /// getEncoding
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        private static Encoding GetEncodingTypeFormCharterSet(string charterSet, bool returnDefaultEncodingIfFailed)
        {
            Encoding encode = null;
            try
            {
                encode = Encoding.GetEncoding(charterSet ?? "UTF-8");
            }
            catch
            {
                if (returnDefaultEncodingIfFailed)
                {
                    encode = Encoding.GetEncoding("gb2312");//Legacy comment translated to English.
                }
            }
            #region Legacy section
            //switch (charterSet.ToUpper())
            //{
            //    case "GB2312":
            //        encode = Encoding.GetEncoding("gb2312");
            //        break;
            //    case "UTF-8":
            //    case "ISO-8859-1":
            //        encode = Encoding.UTF8;
            //        break;
            //    default:
            //        encode = System.Text.Encoding.Default;
            //        break;
            //}
            #endregion

            return encode;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="receiveStream"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        private static string GetTrueEncodingHtml(Stream receiveStream, ref Encoding encode)
        {
            //encode = Encoding.GetEncoding("gb2312");
            using (StreamReader sr = new StreamReader(receiveStream, encode))
            {
                //string html = sr.ReadToEnd();

                char[] readbuffer = new char[256];
                int n = sr.Read(readbuffer, 0, 256);
                //StringBuilder htmlSB = new StringBuilder();
                string html = null;

                int charCount = 0;
                bool charterChecked = false;

                List<char> cacheChar = new List<char>();
                cacheChar.AddRange(readbuffer.Clone() as char[]);
                while (n > 0)
                {
                    charCount += 256;
                    string str = new string(readbuffer, 0, n);
                    html += str;

                    if (!charterChecked && charCount > 1000)
                    {
                        charterChecked = true;
                        //Legacy comment translated to English.
                        string newCharSet = Regex.Match(html, charSetPttern).Groups["charset"].Value;//Legacy comment translated to English.
                        if (!newCharSet.IsNullOrEmpty())
                        {
                            Encoding newEncoding = GetEncodingTypeFormCharterSet(newCharSet, false);
                            if (newEncoding != null)
                            {
                                if (newEncoding.WebName == encode.WebName)//Legacy comment translated to English.
                                {
                                    html += sr.ReadToEnd();//Legacy comment translated to English.
                                    break;
                                }
                                else
                                {
                                    //Legacy comment translated to English.
                                    //byte[] newBytes = cacheChar.Select(z=> Convert.FromBase64CharArray(cacheChar.ToArray(), 0, cacheChar.Count);
                                    byte[] newBytes = encode.GetBytes(html);
                                    string previousHtml = newEncoding.GetString(newBytes, 0, html.Length);//Legacy comment translated to English.
                                    encode = newEncoding;//Legacy comment translated to English.
                                    using (StreamReader newSr = new StreamReader(receiveStream, encode))
                                    {
                                        html = previousHtml + newSr.ReadToEnd();
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    n = sr.Read(readbuffer, 0, 256);

                    if (!charterChecked)
                    {
                        cacheChar.AddRange(readbuffer.Clone() as char[]);
                    }
                }

                return html;//Legacy comment translated to English.
            }
            #region Legacy section
            //StringBuilder htmlSB = new StringBuilder();
            //StreamReader sr = new StreamReader(receiveStream,encode);
            ////List<byte[]> bufferList = new List<byte[]>();

            ////byte[] readbufferByte = new byte[256];
            //char[] readbuffer = new char[256];
            //int n = sr.Read(readbuffer, 0, 256);
            ////bufferList.Add(readbufferByte.Clone() as byte[]);
            //while (n > 0)
            //{
            //    string str = new string(readbuffer, 0, n);
            //    htmlSB.Append(str);
            //    n = sr.Read(readbuffer, 0, 256);
            //    //bufferList.Add(readbufferByte.Clone() as byte[]);
            //}
            //sr.Dispose();

            ///Legacy comment translated to English.
            ///Legacy comment translated to English.
            ////StringBuilder sbTempHtml = new StringBuilder();
            ////for (int i = 0; i < Math.Min(15, bufferList.Count); i++)
            ////{
            ////    sbTempHtml.Append(encode.GetString(bufferList[i]));
            ////}

            ////string tempHtml = sbTempHtml.ToString();
            ////string pattern = @"(?i)\bcharset=(?<charset>[-a-zA-Z_0-9]+)";
            ////string charterSet = Regex.Match(tempHtml, pattern).Groups["charset"].Value;
            ////if (!charterSet.IsNullOrEmpty())
            ////{
            ////    Encoding finalEncoding = GetEncodingTypeFormCharterSet(charterSet);
            ////    if (finalEncoding != null && finalEncoding != encode)
            ////    {
            ///Legacy comment translated to English.
            ////    }
            ////}

            ////foreach (var item in bufferList)
            ////{
            ///Legacy comment translated to English.
            ////}

            //return htmlSB.ToString();
            #endregion
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private void StopBuild()
        {
            this._currentUrlBuildStop = true;
            //this._currentAvaliableUrlTemp.Clear();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="endStr"></param>
        /// <returns></returns>
        private bool EndWithAny(string str, IEnumerable<string> endStr)
        {
            foreach (var item in endStr)
            {
                if (str.EndsWith(item))
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool IsAvailableUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || string.IsNullOrWhiteSpace(uri.Host)
                || !string.IsNullOrEmpty(uri.UserInfo))
            {
                return false;
            }

            if (IPAddress.TryParse(uri.Host, out var address))
            {
                return !IsPrivateOrLocalAddress(address);
            }

            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Host, "localhost.localdomain", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                // Fail closed if a hostname resolves to any private, loopback,
                // link-local, multicast, or otherwise local address.
                var addresses = Dns.GetHostAddresses(uri.Host);
                return addresses.Length > 0 && addresses.All(IsPublicAddress);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPublicAddress(IPAddress address)
        {
            return !IsPrivateOrLocalAddress(address);
        }

        private static bool IsPrivateOrLocalAddress(IPAddress address)
        {
            if (address == null)
            {
                return true;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (IPAddress.IsLoopback(address)
                || IPAddress.Any.Equals(address)
                || IPAddress.IPv6Any.Equals(address)
                || address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast)
            {
                return true;
            }

            var bytes = address.GetAddressBytes();
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && bytes.Length == 4)
            {
                return bytes[0] == 0
                    || bytes[0] == 10
                    || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
                    || bytes[0] == 127
                    || (bytes[0] == 169 && bytes[1] == 254)
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168)
                    || (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0)
                    || (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2)
                    || (bytes[0] == 198 && bytes[1] >= 18 && bytes[1] <= 19)
                    || (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100)
                    || (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113)
                    || bytes[0] >= 224;
            }

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && bytes.Length == 16)
            {
                // IPv6 unique-local (fc00::/7) and documentation (2001:db8::/32)
                // addresses are not valid public crawl targets.
                return (bytes[0] & 0xfe) == 0xfc
                    || bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8;
            }

            return false;
        }


        #region Legacy section
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="keywords"></param>
        /// <returns></returns>
        private List<FilterOmitWord> GetFilterOmitWords(List<string> keywords)
        {
            List<FilterOmitWord> result = new List<FilterOmitWord>();
            foreach (var keyword in keywords)
            {
                var value = this.GetOperatorKind(keyword);
                if (value.Operatorkind != FilterOmitWord.OperatorKind.None &&
                    result.Count(z => z.Keyword == keyword && z.Operatorkind == value.Operatorkind) == 0)
                {
                    result.Add(value);
                }
            }
            return result;
        }

        private FilterOmitWord GetOperatorKind(string keywordWithOperator)
        {
            keywordWithOperator = keywordWithOperator.ToUpper().Trim();//Legacy comment translated to English.

            var allKeywordsOperator = new[] { "?", "^", "$", "=" };//Legacy comment translated to English.
            var needKeywordsOperator = new[] { "^", "$", "=" };//Legacy comment translated to English.

            FilterOmitWord result = new FilterOmitWord("error", FilterOmitWord.OperatorKind.None);
            if (keywordWithOperator.IsNullOrEmpty())
            {
                return result;//Legacy comment translated to English.
            }

            if (keywordWithOperator.Length == 1 && needKeywordsOperator.Contains(keywordWithOperator))
            {
                return result;//Legacy comment translated to English.
            }

            string firstChar = keywordWithOperator.Substring(0, 1);//Legacy comment translated to English.

            if (allKeywordsOperator.Contains(firstChar))
            {
                //Legacy comment translated to English.
                string keyword = keywordWithOperator.Substring(1, keywordWithOperator.Length - 1);//Legacy comment translated to English.
                switch (firstChar)
                {
                    case "?":
                        result = new FilterOmitWord(keyword, FilterOmitWord.OperatorKind.ContainsParameters);
                        break;
                    case "^":
                        result = new FilterOmitWord(keyword, FilterOmitWord.OperatorKind.StartWith);
                        break;
                    case "$":
                        result = new FilterOmitWord(keyword, FilterOmitWord.OperatorKind.EndWith);
                        break;
                    case "=":
                        result = new FilterOmitWord(keyword, FilterOmitWord.OperatorKind.Equal);
                        break;
                    default:
                        throw new Exception("未知操作符");
                }
            }
            else
            {
                //Legacy comment translated to English.
                result = new FilterOmitWord(keywordWithOperator, FilterOmitWord.OperatorKind.Contains);
            }
            return result;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool ContainsFilterOmitWord(string url)
        {
            if (url.IsNullOrEmpty())
            {
                return false;
            }

            url = url.ToUpper();
            foreach (var filterWord in this.FilterOmitWords)
            {
                switch (filterWord.Operatorkind)
                {
                    case FilterOmitWord.OperatorKind.Contains:
                        if (url.Contains(filterWord.Keyword)) { return true; }
                        break;
                    case FilterOmitWord.OperatorKind.ContainsParameters:
                        int index = url.IndexOf("?");
                        if (index != -1)
                        {
                            url = url.Substring(index, url.Length - index);
                            if (url.Contains(filterWord.Keyword) || filterWord.Keyword.IsNullOrEmpty()) { return true; }
                        }
                        break;
                    case FilterOmitWord.OperatorKind.StartWith:
                        if (url.StartsWith(filterWord.Keyword)) { return true; }
                        break;
                    case FilterOmitWord.OperatorKind.EndWith:
                        if (url.EndsWith(filterWord.Keyword)) { return true; }
                        break;
                    case FilterOmitWord.OperatorKind.Equal:
                        if (url.Equals(filterWord.Keyword)) { return true; }
                        break;
                    case FilterOmitWord.OperatorKind.None:
                    default:
                        continue;
                }
            }

            return false;
        }
        #endregion

        public void InitializeForTest()
        {
            #region Legacy section
            _currentProtocol = "http";
            _currentDomain = "www.baidu.com";
            _currentOriginalFullDomain = "http://www.baidu.com";
            #endregion
        }
    }
}
