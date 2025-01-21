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
        /// 重置
        /// </summary>
        private void ResetUrlPath(string rootDomain)
        {
            _urlPath.Clear();
            _urlPath.Add(new UrlPathCollection(rootDomain, 0, new List<string>()));//定义域名级别
            for (int i = 1; i <= _maxDeep; i++)
            {
                _urlPath.Add(new UrlPathCollection(null, i, new List<string>()));
            }
        }

        /// <summary>
        /// 获取带协议完整域名，如Http://www.senparc.com
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
        /// 获取域名，如www.senparc.com
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
        /// 获取协议，如http
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
        /// 去掉Url末尾的/。如果为../或/，则不过滤
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
                    url = url.Substring(0, url.Length - 1);//去掉末尾的“/”
                }
                return url;
            }
        }

        /// <summary>
        /// 去掉Url开头的/，如果字符串等于/，则返回/
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
                    url = url.Substring(1, url.Length - 1);//去掉末尾的“/”
                }
                return url;
            }
        }

        /// <summary>
        /// 获取包含[协议]://[域名]/[地址]的完整Url
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
                //parentUrl = this.RemoveUrlEndingSlash(parentUrl);//parent末尾有没有/要分情况
                //过滤特殊协议
                if (newUrl.StartsWith("mailto:")
                        || newUrl.StartsWith("javascript:")
                        || newUrl.StartsWith("ftp://")
                        || newUrl.StartsWith("skype:")
                        || newUrl.StartsWith("callto:")//同skype:，格式：callto://szw2003/
                        || newUrl.StartsWith("tencent:")
                        || newUrl.StartsWith("msnim:")
                        || newUrl.StartsWith("file:")
                        || newUrl.StartsWith("ymsgr:") //TODO:更多协议过滤
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

                if (Regex.IsMatch(newUrl.ToUpper(), "^HTTP(S){0,1}://", RegexOptions.IgnoreCase))//以http(s)开头
                {
                    if (!IsAvailableUrl(newUrl))
                    {
                        return null;//不符合要求的url,如http://non
                    }

                    string newUrlDomain = GetDomain(newUrl);
                    if (newUrlDomain == null || !_currentDomain.ToUpper().Contains(newUrlDomain.ToUpper()))
                    {
                        return null;//新域名为空，或连接到其他域名
                    }

                    //TODO:继续完善允许二级域名的情况（需要考虑.com.cn和.com等域名的情况）
                    //string topDomain = domain.Substring(domain.IndexOf("."), domain.Length - domain.IndexOf("."));//如:senparc.com过滤后为http://.com
                    //if (!topDomain.Contains("."))
                    //{
                    //    topDomain = domain;//TODO:仍无法过滤类似abc.(com.cn)的情况
                    //}
                    //if (domain == null || (domain != _currentDomain && !_currentDomain.ToUpper().EndsWith(topDomain.ToUpper())))//二级域名予以承认
                    //{
                    //    return null;//domain==null的情况会发生在href=“http://”或“http://#/a.htm”的情况下
                    //}

                    //if (!parentUrl.IsNullOrEmpty() && newUrlDomain.ToUpper() != _currentDomain.ToUpper())
                    //{
                    //    return null;//跳转到不同网站
                    //}

                    tempFullUrl = newUrl;
                }
                else
                {
                    tempFullUrl = parentUrl;//当前父层Url(不能去掉末尾/)

                    string parentFullDomain = GetFullDomain(parentUrl);//复层链接完整Url

                    //本地路径
                    if (newUrl.StartsWith("/"))
                    {
                        tempFullUrl = parentFullDomain + newUrl;//绝对路径(e.g. http://www.senparc.com/)
                    }
                    else if (newUrl.StartsWith("?"))//参数形式，直接加到当前路径结尾
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
                    else//其他开头，如相对路径
                    {
                        if (parentUrl.EndsWith("/"))//上级目录以“/”结尾
                        {
                            tempFullUrl += newUrl;
                        }
                        else
                        {
                            if (Regex.IsMatch(tempFullUrl.ToUpper(), @"^HTTP(S)?://", RegexOptions.IgnoreCase) && tempFullUrl.LastIndexOf("/") <= 7)
                            {
                                //TODO:一般搜索引擎会认为这是两个不同的Url，这里不处理也可以
                                tempFullUrl += "/";//出现http://www.senparc.com的情况，补足为http://www.senparc.com/
                            }
                            tempFullUrl = tempFullUrl.Substring(0, tempFullUrl.LastIndexOf("/")) + "/" + newUrl;//（TODO:可以考虑中间出现./的情况）       
                        }
                    }


                    if (tempFullUrl.Contains("/../"))//或newUrl.Contains("../")
                    {
                        if (_currentPageCount > 0 && _currentUrl.ToUpper().StartsWith(newUrl.ToUpper()))
                        {
                            return null;//如果当前查询到的URL是当前域名根目录，则过滤
                        }

                        int domainLength = _currentOriginalFullDomain.Length;
                        while (tempFullUrl.Contains("/../"))
                        {
                            int indexOfParentMark = tempFullUrl.IndexOf("/../");//出现../的Index
                            string parentUrlPath;
                            if (indexOfParentMark <= domainLength)//出现了http://www.senparc.com/../这样的情况，过滤掉就行
                            {
                                parentUrlPath = _currentOriginalFullDomain;
                            }
                            else
                            {
                                int indexOfParentLevel = tempFullUrl.Substring(0, indexOfParentMark).LastIndexOf("/");//../之前的最近一个/
                                parentUrlPath = tempFullUrl.Substring(0, indexOfParentLevel);//上一级
                            }

                            string afterPath = tempFullUrl.Substring(indexOfParentMark + 4, tempFullUrl.Length - indexOfParentMark - 4);//剩下部分
                            tempFullUrl = parentUrlPath + "/" + afterPath;
                        }

                        if (!tempFullUrl.ToUpper().StartsWith(_currentProtocol.ToUpper() + "://" + _currentDomain.ToUpper()))
                        {
                            return null;//路径描述有误，默认为当前域名根目录，但是由于已经收录，所以过滤。（如出现了http://www.senparc.com/../../的情况）
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
        /// 抓取单个Url（无统计数据）
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
        /// 爬行Url，并带回UrlData信息
        /// </summary>
        /// <param name="url"></param>
        /// <returns>返回UrlData信息，如果不符合爬行条件（如CotentType类型不符合等），则返回null</returns>
        private async Task<UrlData> CrawlUrl(string url, string parentUrl, int deep, bool singleUrl, string linkText)
        {
            //BMK:此方法收集时有的网站会发生“未将对象引用设置到对象的实例”错误，如：http://www.w2tx.cn/batch.common.php?action=viewnews&op=up&itemid=77&catid=70

            UrlData urlData = null;
            HttpResponseMessage webResponse = null;
            DateTime requestStartTime = DateTime.MinValue;
            DateTime requestEndTime = DateTime.MinValue;
            try
            {
                //已改为在外部判断
                //if (deep > _maxDeep || _currentUrls.Count >= _maxPageCount)
                //{
                //    return null;//超出范围，不予收录
                //}

                url = RemoveUrlEndingSlash(url);//去掉末尾的“/”

                if (!this.IsAvailableUrl(url)//非标准Url
                    || this.ContainsFilterOmitWord(url)//包含过滤关键字
                    || this._currentUrls.ContainsKey(url) //已经收录
                    )
                {
                    return null;
                }

                if (!this._currentAvaliableUrlTemp.ContainsKey(url))
                {
                    this._currentAvaliableUrlTemp.Add(url, new AvailableUrl(url, parentUrl, deep, linkText, AvailableUrlStatus.Started));//添加到待选Url
                }
                else if (this._currentAvaliableUrlTemp[url].Status != AvailableUrlStatus.UnStart)
                {
                    return null;//已经由别的线程开始，或已经完成
                }

                //开始访问
                var responseResult = await RequestPage(url);//爬行获取webReponse
                webResponse = responseResult.response;
                requestStartTime = responseResult.requestStartTime;
                requestEndTime = responseResult.requestEndTime;

                this._currentAvaliableUrlTemp[url].Status = AvailableUrlStatus.Finished;//标记完成

                //比对URL，如果发生302或301（自动添加末尾/）的情况
                if (url.ToUpper() != webResponse.RequestMessage.RequestUri.AbsoluteUri.ToUpper())
                {
                    //发生改变
                    this._currentAvaliableUrlTemp[url].Url = webResponse.RequestMessage.RequestUri.AbsoluteUri;//更新AvaliableUrlTemp中的信息。
                    url = webResponse.RequestMessage.RequestUri.AbsoluteUri;//url调整为新的url
                    if (this._currentUrls.ContainsKey(url) || this.ContainsFilterOmitWord(url))
                    {
                        return null;//已经收录过，或包含过滤词，停止收录
                    }
                    else if (!url.IsNullOrEmpty() && !_currentDomain.ToUpper().Contains(this.GetDomain(url).ToUpper()))
                    {
                        return null;//如果跳转到其他网站网页，则终止
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

                //判断Gzip压缩
                string ce = webResponse.Content.Headers.ContentEncoding.FirstOrDefault();
                Stream receiveStream = null;
                receiveStream = SenMapicUtility.GetReceiveStream(ce, webResponse);

                //判断字符集
                string contentType = webResponse.Content.Headers.ContentType?.MediaType;
                string charterSet = webResponse.Content.Headers.ContentType?.CharSet;

                //从Content-Type中获取charterSet。出于效率考虑不使用LINQ
                charterSet = GetCharterSetFromContentType(charterSet);

                if (!contentType.ToUpper().StartsWith("TEXT/"))
                {
                    return null;
                }

                Encoding encode = GetEncodingTypeFormCharterSet(charterSet, true);
                string htmlTotal = GetTrueEncodingHtml(receiveStream, ref encode) ?? ""; //获取html TODO:如果是301跳转，且html无内容，可能导致htmlTotal中为空
                receiveStream.Dispose();

                int htmlByteCount = htmlTotal.IsNullOrEmpty() ? 0 : encode.GetByteCount(htmlTotal); //System.Text.Encoding.Default.GetByteCount(htmlTotal);
                string titleHtml = htmlTotal.Length > 1000 ? htmlTotal.Substring(0, 1000) : htmlTotal;//取Html前一部分（只用于取网页Title），否则当网页非常大的时候（出现过790k），服务器运算时间过长
                urlData = new UrlData(url, deep, htmlTotal, titleHtml, (int)HttpStatusCode.OK, (double)htmlByteCount / 1024, null, (int)TimeSpan.FromTicks(_requestPageTicks).TotalMilliseconds, linkText);

                #region 调试 - 下载记录时间
                //DateTime dt2 = DateTime.Now;
                //LogUtility.SitemapLogger.Debug("url:{0},接收时间：{1},字符数：{2}。平均1000字符耗时：{3}".With(url,
                //    (dt2 - dt1).TotalMilliseconds.ToString("###,###ms"),
                //    webResponse.ContentLength,
                //    ((dt2 - dt1).TotalMilliseconds / ((webResponse.ContentLength == 0 ? -1 : webResponse.ContentLength) / 1000)).ToString("###,###ms")));

                //if (_currentDeep == _maxDeep || _currentUrls.Count == _maxPageCount)
                //{
                //    return urlData;//正常返回、收录
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
                else //if (e.Status == WebExceptionStatus.UnknownError)//不使用Timeout，为了兼容SL
                {
                    result = 9;

                    //如果网页超时严重，则强制退出
                    if (this._currentUrls.Count(z => z.Value.Result == 9) >= this._maxTimeoutTimes)
                    {
                        this.StopBuild();
                    }
                }
                //LogUtility.WebLogger.Error("SitemapDebug异常({0})：{1}".With(url, e.Message), e);
                return new UrlData(url, deep, "", "", result, 0, null, linkText);
            }
            catch (Exception e)
            {
                var ex = new Exception($"SitemapDebug异常({url})", e);
                //记录异常
                SenparcTrace.BaseExceptionLog(ex);
                return new UrlData(url, deep, "", "", 0, 0, null, linkText);
            }
            finally
            {
                if (webResponse != null)
                {
                    webResponse.Dispose();//关闭链接
                }

                if (urlData != null)
                {
                    TotalPageSizeKB += urlData.SizeKB;

                    int currentRequestMilliSeconds = (int)(requestEndTime - requestStartTime).TotalMilliseconds;
                    _requestPageCount++;
                    _requestPageTicks += (requestEndTime - requestStartTime).Ticks;//添加时差

                    urlData.ResponseMillionSeconds = currentRequestMilliSeconds;
                }

                if (!singleUrl && (DateTime.Now - this.CurrentSiteStartTime).TotalMinutes >= MaxBuildMinutesForSingleSite)
                {
                    this.StopBuild();
                    SenparcTrace.SendCustomLog("SiteMap", $"SitemapBuild超时退出（{MaxBuildMinutesForSingleSite}分钟）。当前Url:{url}");
                }
            }
            return urlData;//返回收集完成的UrlData
        }

        /// <summary>
        /// 从Content-Type中获取CharterSet,如果无法获取，返回null。出于效率考虑不使用LINQ。
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
        /// 获取Encoding
        /// </summary>
        /// <param name="charterSet">charterSet字符串，如utf-8</param>
        /// <param name="returnDefaultEncodingIfFailed">当为True时，如果获取Encoding失败，返回Encoding.Default，否则返回null</param>
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
                    encode = Encoding.GetEncoding("gb2312");//SL中没有Default，这里也可以使用utf-8(国内网站建议使用gb312)
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
        /// 从响应流中以正确的编码返回内容
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
                        //判断编码
                        string newCharSet = Regex.Match(html, charSetPttern).Groups["charset"].Value;//匹配Html中的charset
                        if (!newCharSet.IsNullOrEmpty())
                        {
                            Encoding newEncoding = GetEncodingTypeFormCharterSet(newCharSet, false);
                            if (newEncoding != null)
                            {
                                if (newEncoding.WebName == encode.WebName)//这里不使用BodyName是为了兼容SL。SL中没有BodyName
                                {
                                    html += sr.ReadToEnd();//编码相同
                                    break;
                                }
                                else
                                {
                                    //编码不同
                                    //byte[] newBytes = cacheChar.Select(z=> Convert.FromBase64CharArray(cacheChar.ToArray(), 0, cacheChar.Count);
                                    byte[] newBytes = encode.GetBytes(html);
                                    string previousHtml = newEncoding.GetString(newBytes, 0, html.Length);//不加0,,html.Length参数，在SL下会发生错误：CS0122  Encoding.GetString(byte[]) is inaccessible due to its protection level
                                    encode = newEncoding;//转换编码
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

                return html;//TODO:如果301跳转，并且只靠Header信息，没有html说明，可能导致无法获取html内容
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

            //////最终确认Encoding
            //////获取前10个buffer，使用系统查询出来的encoding，查看是否有其他编码（权衡效率）
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
            ////        encode = finalEncoding;//编码不同则根据后者确定
            ////    }
            ////}

            ////foreach (var item in bufferList)
            ////{
            ////    htmlSB.Append(encode.GetString(item));//TODO:內容完整性有損失，有待調整
            ////}

            //return htmlSB.ToString();
            #endregion
        }

        /// <summary>
        /// 停止收集（要等下一个Url开始才会响应）
        /// </summary>
        private void StopBuild()
        {
            this._currentUrlBuildStop = true;
            //this._currentAvaliableUrlTemp.Clear();
        }

        /// <summary>
        /// 以任意关键字结尾
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
        /// 是否符合Url格式标准
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool IsAvailableUrl(string url)
        {
            //上一版本正则：^HTTP(S)?://([\w-]+\.)+[\w-]+(:\d+)?(/[\w- ./?%&=]*)?
            return Regex.IsMatch(url.ToUpper(), @"^HTTP(S)?://((([\w-]+\.)+[\w-]+)|(localhost))(:\d+)?(/[\w- ./?%&=]*)?", RegexOptions.IgnoreCase);//^表示必须以Http开始
        }


        #region 过滤关键字
        /// <summary>
        /// 获取过滤关键字列表
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
            keywordWithOperator = keywordWithOperator.ToUpper().Trim();//转成大写

            var allKeywordsOperator = new[] { "?", "^", "$", "=" };//所有关键字
            var needKeywordsOperator = new[] { "^", "$", "=" };//必须要加关键字的操作符

            FilterOmitWord result = new FilterOmitWord("error", FilterOmitWord.OperatorKind.None);
            if (keywordWithOperator.IsNullOrEmpty())
            {
                return result;//表达式为空
            }

            if (keywordWithOperator.Length == 1 && needKeywordsOperator.Contains(keywordWithOperator))
            {
                return result;//表达式错误
            }

            string firstChar = keywordWithOperator.Substring(0, 1);//第一个字符

            if (allKeywordsOperator.Contains(firstChar))
            {
                //包含操作符
                string keyword = keywordWithOperator.Substring(1, keywordWithOperator.Length - 1);//关键字
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
                //普通情况
                result = new FilterOmitWord(keywordWithOperator, FilterOmitWord.OperatorKind.Contains);
            }
            return result;
        }

        /// <summary>
        /// 是否包含过滤关键字
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
