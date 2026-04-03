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
        ///reset
        /// </summary>
        private void ResetUrlPath(string rootDomain)
        {
            _urlPath.Clear();
            _urlPath.Add(new UrlPathCollection(rootDomain, 0, new List<string>()));//Define domain name level
            for (int i = 1; i <= _maxDeep; i++)
            {
                _urlPath.Add(new UrlPathCollection(null, i, new List<string>()));
            }
        }

        /// <summary>
        /// Get the complete domain name with protocol, such as http://www.senparc.com
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
        /// Get a domain name, such as www.senparc.com
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
        /// Get the protocol, such as http
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
        /// Remove the / at the end of Url. If ../ or /, no filtering
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
                    url = url.Substring(0, url.Length - 1);//Remove the "/" at the end
                }
                return url;
            }
        }

        /// <summary>
        /// Remove the / at the beginning of Url. If the string is equal to /, return /
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
                    url = url.Substring(1, url.Length - 1);//Remove the "/" at the end
                }
                return url;
            }
        }

        /// <summary>
        /// Get the complete Url containing [protocol]://[domain name]/[address]
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
                //parentUrl = this.RemoveUrlEndingSlash(parentUrl);//It depends on the situation whether there is / at the end of parent
                //Filter special protocols
                if (newUrl.StartsWith("mailto:")
                        || newUrl.StartsWith("javascript:")
                        || newUrl.StartsWith("ftp://")
                        || newUrl.StartsWith("skype:")
                        || newUrl.StartsWith("callto:")//Same as skype:, format: callto://szw2003/
                        || newUrl.StartsWith("tencent:")
                        || newUrl.StartsWith("msnim:")
                        || newUrl.StartsWith("file:")
                        || newUrl.StartsWith("ymsgr:") //TODO: More protocol filtering
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

                if (Regex.IsMatch(newUrl.ToUpper(), "^HTTP(S){0,1}://", RegexOptions.IgnoreCase))//Start with http(s)
                {
                    if (!IsAvailableUrl(newUrl))
                    {
                        return null;//URLs that do not meet the requirements, such as http://non
                    }

                    string newUrlDomain = GetDomain(newUrl);
                    if (newUrlDomain == null || !_currentDomain.ToUpper().Contains(newUrlDomain.ToUpper()))
                    {
                        return null;//The new domain name is empty or connected to another domain name
                    }

                    //TODO: Continue to improve the situation of allowing second-level domain names (need to consider the situation of domain names such as .com.cn and .com)
                    //string topDomain = domain.Substring(domain.IndexOf("."), domain.Length - domain.IndexOf("."));//For example: senparc.com is filtered to http://.com
                    //if (!topDomain.Contains("."))
                    //{
                    //    topDomain = domain;//TODO: Still unable to filter situations like abc.(com.cn)
                    //}
                    //if (domain == null || (domain != _currentDomain && !_currentDomain.ToUpper().EndsWith(topDomain.ToUpper())))//Second-level domain name is recognized
                    //{
                    //    return null;//domain==null will occur when href="http://" or "http://#/a.htm"
                    //}

                    //if (!parentUrl.IsNullOrEmpty() && newUrlDomain.ToUpper() != _currentDomain.ToUpper())
                    //{
                    //    return null;//Jump to different websites
                    //}

                    tempFullUrl = newUrl;
                }
                else
                {
                    tempFullUrl = parentUrl;//Current parent layer Url (the trailing / cannot be removed)

                    string parentFullDomain = GetFullDomain(parentUrl);//Multi-layer link complete URL

                    //local path
                    if (newUrl.StartsWith("/"))
                    {
                        tempFullUrl = parentFullDomain + newUrl;//Absolute path (e.g. http://www.senparc.com/)
                    }
                    else if (newUrl.StartsWith("?"))//Parameter form, added directly to the end of the current path
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
                    else//Other beginnings, such as relative paths
                    {
                        if (parentUrl.EndsWith("/"))//The parent directory ends with "/"
                        {
                            tempFullUrl += newUrl;
                        }
                        else
                        {
                            if (Regex.IsMatch(tempFullUrl.ToUpper(), @"^HTTP(S)?://", RegexOptions.IgnoreCase) && tempFullUrl.LastIndexOf("/") <= 7)
                            {
                                //TODO: Generally, search engines will think that these are two different URLs, so it is okay not to process them here.
                                tempFullUrl += "/";//If http://www.senparc.com appears, the supplement is http://www.senparc.com/
                            }
                            tempFullUrl = tempFullUrl.Substring(0, tempFullUrl.LastIndexOf("/")) + "/" + newUrl;//(TODO: You can consider the situation where ./ appears in the middle)       
                        }
                    }


                    if (tempFullUrl.Contains("/../"))//or newUrl.Contains("../")
                    {
                        if (_currentPageCount > 0 && _currentUrl.ToUpper().StartsWith(newUrl.ToUpper()))
                        {
                            return null;//If the currently queried URL is the root directory of the current domain name, filter
                        }

                        int domainLength = _currentOriginalFullDomain.Length;
                        while (tempFullUrl.Contains("/../"))
                        {
                            int indexOfParentMark = tempFullUrl.IndexOf("/../");//The Index of ../ appears
                            string parentUrlPath;
                            if (indexOfParentMark <= domainLength)//If a situation like http://www.senparc.com/../ occurs, just filter it out.
                            {
                                parentUrlPath = _currentOriginalFullDomain;
                            }
                            else
                            {
                                int indexOfParentLevel = tempFullUrl.Substring(0, indexOfParentMark).LastIndexOf("/");//../most recent one before/
                                parentUrlPath = tempFullUrl.Substring(0, indexOfParentLevel);//Previous level
                            }

                            string afterPath = tempFullUrl.Substring(indexOfParentMark + 4, tempFullUrl.Length - indexOfParentMark - 4);//remaining part
                            tempFullUrl = parentUrlPath + "/" + afterPath;
                        }

                        if (!tempFullUrl.ToUpper().StartsWith(_currentProtocol.ToUpper() + "://" + _currentDomain.ToUpper()))
                        {
                            return null;//The path description is incorrect. It defaults to the root directory of the current domain name, but since it has been included, it is filtered. (For example, http://www.senparc.com/../../ appears)
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
        /// Fetch a single URL (no statistics)
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
        /// Crawl Url and bring back UrlData information
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Returns UrlData information. If the crawling conditions are not met (such as CotentType type does not meet, etc.), null is returned</returns>
        private async Task<UrlData> CrawlUrl(string url, string parentUrl, int deep, bool singleUrl, string linkText)
        {
            //BMK: When collecting with this method, some websites will encounter the error "object reference is not set to an instance of the object", such as: http://www.w2tx.cn/batch.common.php?action=viewnews&op=up&itemid=77&catid=70

            UrlData urlData = null;
            HttpResponseMessage webResponse = null;
            DateTime requestStartTime = DateTime.MinValue;
            DateTime requestEndTime = DateTime.MinValue;
            try
            {
                //Has been changed to be judged externally
                //if (deep > _maxDeep || _currentUrls.Count >= _maxPageCount)
                //{
                //    return null;//Out of range, not included
                //}

                url = RemoveUrlEndingSlash(url);//Remove the "/" at the end

                if (!this.IsAvailableUrl(url)//Non-standard URL
                    || this.ContainsFilterOmitWord(url)//Contains filter keywords
                    || this._currentUrls.ContainsKey(url) //Already included
                    )
                {
                    return null;
                }

                if (!this._currentAvaliableUrlTemp.ContainsKey(url))
                {
                    this._currentAvaliableUrlTemp.Add(url, new AvailableUrl(url, parentUrl, deep, linkText, AvailableUrlStatus.Started));//Add to selected Url
                }
                else if (this._currentAvaliableUrlTemp[url].Status != AvailableUrlStatus.UnStart)
                {
                    return null;//Has been started by another thread, or has been completed
                }

                //Start visiting
                var responseResult = await RequestPage(url);//Crawl to get webReponse
                webResponse = responseResult.response;
                requestStartTime = responseResult.requestStartTime;
                requestEndTime = responseResult.requestEndTime;

                this._currentAvaliableUrlTemp[url].Status = AvailableUrlStatus.Finished;//Mark complete

                //Compare the URL, if 302 or 301 occurs (automatically add the trailing /)
                if (url.ToUpper() != webResponse.RequestMessage.RequestUri.AbsoluteUri.ToUpper())
                {
                    //change
                    this._currentAvaliableUrlTemp[url].Url = webResponse.RequestMessage.RequestUri.AbsoluteUri;//Update the information in AvailableUrlTemp.
                    url = webResponse.RequestMessage.RequestUri.AbsoluteUri;//url adjusted to new url
                    if (this._currentUrls.ContainsKey(url) || this.ContainsFilterOmitWord(url))
                    {
                        return null;//Already included, or contains filter words, stop collecting
                    }
                    else if (!url.IsNullOrEmpty() && !_currentDomain.ToUpper().Contains(this.GetDomain(url).ToUpper()))
                    {
                        return null;//If it jumps to other website pages, it will be terminated.
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

                //Determine Gzip compression
                string ce = webResponse.Content.Headers.ContentEncoding.FirstOrDefault();
                Stream receiveStream = null;
                receiveStream = SenMapicUtility.GetReceiveStream(ce, webResponse);

                //Determine character set
                string contentType = webResponse.Content.Headers.ContentType?.MediaType;
                string charterSet = webResponse.Content.Headers.ContentType?.CharSet;

                //Get charterSet from Content-Type. Not using LINQ for efficiency reasons
                charterSet = GetCharterSetFromContentType(charterSet);

                if (!contentType.ToUpper().StartsWith("TEXT/"))
                {
                    return null;
                }

                Encoding encode = GetEncodingTypeFormCharterSet(charterSet, true);
                string htmlTotal = GetTrueEncodingHtml(receiveStream, ref encode) ?? ""; //Get html TODO: If it is a 301 jump and the html has no content, the htmlTotal may be empty.
                receiveStream.Dispose();

                int htmlByteCount = htmlTotal.IsNullOrEmpty() ? 0 : encode.GetByteCount(htmlTotal); //System.Text.Encoding.Default.GetByteCount(htmlTotal);
                string titleHtml = htmlTotal.Length > 1000 ? htmlTotal.Substring(0, 1000) : htmlTotal;//Get the first part of Html (only used to get the title of the web page), otherwise when the web page is very large (over 790k), the server operation time will be too long
                urlData = new UrlData(url, deep, htmlTotal, titleHtml, (int)HttpStatusCode.OK, (double)htmlByteCount / 1024, null, (int)TimeSpan.FromTicks(_requestPageTicks).TotalMilliseconds, linkText);

                #region 调试 - 下载记录时间
                //DateTime dt2 = DateTime.Now;
                //LogUtility.SitemapLogger.Debug("url:{0}, receiving time: {1}, number of characters: {2}. Average time consuming for 1000 characters: {3}".With(url,
                //    (dt2 - dt1).TotalMilliseconds.ToString("###,###ms"),
                //    webResponse.ContentLength,
                //    ((dt2 - dt1).TotalMilliseconds / ((webResponse.ContentLength == 0 ? -1 : webResponse.ContentLength) / 1000)).ToString("###,###ms")));

                //if (_currentDeep == _maxDeep || _currentUrls.Count == _maxPageCount)
                //{
                //    return urlData;//Normal return and collection
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
                else //if (e.Status == WebExceptionStatus.UnknownError)//Do not use Timeout for SL compatibility
                {
                    result = 9;

                    //If the web page times out seriously, force exit
                    if (this._currentUrls.Count(z => z.Value.Result == 9) >= this._maxTimeoutTimes)
                    {
                        this.StopBuild();
                    }
                }
                //LogUtility.WebLogger.Error("SitemapDebug exception ({0}): {1}".With(url, e.Message), e);
                return new UrlData(url, deep, "", "", result, 0, null, linkText);
            }
            catch (Exception e)
            {
                var ex = new Exception($"SitemapDebug异常({url})", e);
                //Log exception
                SenparcTrace.BaseExceptionLog(ex);
                return new UrlData(url, deep, "", "", 0, 0, null, linkText);
            }
            finally
            {
                if (webResponse != null)
                {
                    webResponse.Dispose();//Close link
                }

                if (urlData != null)
                {
                    TotalPageSizeKB += urlData.SizeKB;

                    int currentRequestMilliSeconds = (int)(requestEndTime - requestStartTime).TotalMilliseconds;
                    _requestPageCount++;
                    _requestPageTicks += (requestEndTime - requestStartTime).Ticks;//Add time difference

                    urlData.ResponseMillionSeconds = currentRequestMilliSeconds;
                }

                if (!singleUrl && (DateTime.Now - this.CurrentSiteStartTime).TotalMinutes >= MaxBuildMinutesForSingleSite)
                {
                    this.StopBuild();
                    SenparcTrace.SendCustomLog("SiteMap", $"SitemapBuild超时退出（{MaxBuildMinutesForSingleSite}分钟）。当前Url:{url}");
                }
            }
            return urlData;//Return the collected UrlData
        }

        /// <summary>
        /// Get the CharterSet from Content-Type, if it cannot be obtained, return null. Not using LINQ for efficiency reasons.
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
        /// Get Encoding
        /// </summary>
        /// <param name="charterSet">charterSet string, such as utf-8</param>
        /// <param name="returnDefaultEncodingIfFailed">When True, if obtaining Encoding fails, return Encoding.Default, otherwise return null</param>
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
                    encode = Encoding.GetEncoding("gb2312");//There is no Default in SL, and utf-8 can also be used here (domestic websites recommend using gb312)
                }
            }
            #region 使用Switch判断，已废除此方法
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
        /// Return content from the response stream in the correct encoding
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
                        //judgment coding
                        string newCharSet = Regex.Match(html, charSetPttern).Groups["charset"].Value;//Match charset in Html
                        if (!newCharSet.IsNullOrEmpty())
                        {
                            Encoding newEncoding = GetEncodingTypeFormCharterSet(newCharSet, false);
                            if (newEncoding != null)
                            {
                                if (newEncoding.WebName == encode.WebName)//BodyName is not used here for SL compatibility. There is no BodyName in SL
                                {
                                    html += sr.ReadToEnd();//The encoding is the same
                                    break;
                                }
                                else
                                {
                                    //Encoding is different
                                    //byte[] newBytes = cacheChar.Select(z=> Convert.FromBase64CharArray(cacheChar.ToArray(), 0, cacheChar.Count);
                                    byte[] newBytes = encode.GetBytes(html);
                                    string previousHtml = newEncoding.GetString(newBytes, 0, html.Length);//Without adding 0,,html.Length parameter, an error will occur under SL: CS0122 Encoding.GetString(byte[]) is inaccessible due to its protection level
                                    encode = newEncoding;//Convert encoding
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

                return html;//TODO: If 301 jumps and only relies on Header information without html description, it may result in failure to obtain html content.
            }
            #region 老办法
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

            //////Finally confirm Encoding
            //////Get the first 10 buffers, use the encoding queried by the system to see if there are other encodings (weighing efficiency)
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
            //// encode = finalEncoding;//If the encoding is different, it will be determined based on the latter
            ////    }
            ////}

            ////foreach (var item in bufferList)
            ////{
            //// htmlSB.Append(encode.GetString(item)); //TODO: Content integrity has been lost and needs to be adjusted
            ////}

            //return htmlSB.ToString();
            #endregion
        }

        /// <summary>
        /// Stop collection (will not respond until the next Url starts)
        /// </summary>
        private void StopBuild()
        {
            this._currentUrlBuildStop = true;
            //this._currentAvaliableUrlTemp.Clear();
        }

        /// <summary>
        /// ends with any keyword
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
        /// Whether it complies with Url format standards
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool IsAvailableUrl(string url)
        {
            //Regular version of the previous version: ^HTTP(S)?://([\w-]+\.)+[\w-]+(:\d+)?(/[\w- ./?%&=]*)?
            return Regex.IsMatch(url.ToUpper(), @"^HTTP(S)?://((([\w-]+\.)+[\w-]+)|(localhost))(:\d+)?(/[\w- ./?%&=]*)?", RegexOptions.IgnoreCase);//^ means it must start with Http
        }


        #region 过滤关键字
        /// <summary>
        /// Get filter keyword list
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
            keywordWithOperator = keywordWithOperator.ToUpper().Trim();//Convert to uppercase

            var allKeywordsOperator = new[] { "?", "^", "$", "=" };//all keywords
            var needKeywordsOperator = new[] { "^", "$", "=" };//Operators that require keywords

            FilterOmitWord result = new FilterOmitWord("error", FilterOmitWord.OperatorKind.None);
            if (keywordWithOperator.IsNullOrEmpty())
            {
                return result;//expression is empty
            }

            if (keywordWithOperator.Length == 1 && needKeywordsOperator.Contains(keywordWithOperator))
            {
                return result;//Expression error
            }

            string firstChar = keywordWithOperator.Substring(0, 1);//first character

            if (allKeywordsOperator.Contains(firstChar))
            {
                //contains operator
                string keyword = keywordWithOperator.Substring(1, keywordWithOperator.Length - 1);//Keywords
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
                //Normal situation
                result = new FilterOmitWord(keywordWithOperator, FilterOmitWord.OperatorKind.Contains);
            }
            return result;
        }

        /// <summary>
        /// Whether to include filter keywords
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
            #region 测试用
            _currentProtocol = "http";
            _currentDomain = "www.baidu.com";
            _currentOriginalFullDomain = "http://www.baidu.com";
            #endregion
        }
    }
}
