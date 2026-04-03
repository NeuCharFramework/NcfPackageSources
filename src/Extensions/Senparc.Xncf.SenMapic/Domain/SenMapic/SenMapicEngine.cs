using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    using System.Threading;
    using System.Threading.Tasks;
    using Senparc.CO2NET.Extensions;
    using Senparc.CO2NET.Trace;

    public partial class SenMapicEngine
    {
        private static string _version = "1.7.0.0";//TODO: Currently in Beta 2
        /// <summary>
        ///SenMapicEngine version
        /// </summary>
        public static string Version
        {
            get
            {
                if (_version == null)
                {

                }
                return _version;
            }
        }

        private const string charSetPttern = @"(?i)\bcharset=(?<charset>[-a-zA-Z_0-9]+)";
        private const string urlPattern = "<a[^>]+href=(['\"]{0,1})([^'\"#>\\s\\(\\)]+)(?=['\"#>]{1}).*?>(.*?)</a>";//TODO: Filter such situations in js: "<a href=\""+ url +"\">"; In Joomla, the url has; appears
        private int _maxThread = 2;//Maximum number of threads
        private readonly int _maxTimeoutTimes = 10;//Maximum number of timeout pages (if this number is exceeded, collection will be terminated)
        private readonly int _pageRequestTimeoutMillionSeconds = 1000 * 20;//Page request timeout (milliseconds)
        private readonly IServiceProvider _serviceProvider;
        private int _maxBuildMinutesForSingleSite = 10;//Default maximum collection time (per site)

        private IEnumerable<string> _urls;//Queue Url with included
        private string _currentUrl;//Current search URL
        private bool _currentUrlBuildStop;//Stop current site search
        private string _currentOriginalFullDomain;//Current original complete domain name (http://www.senparc.com)
        private string _currentDomain;//Domain name currently searched
        private string _currentProtocol;//Protocol (http|https)
        private int _currentDeep;//Current collection depth
        private int _currentPageCount;//The number of pages included under the current domain name
        private Dictionary<string, AvailableUrl> _currentAvaliableUrlTemp;//Currently available URL collection TODO:lock
        private Dictionary<string, UrlData> _currentUrls;//All URLs collected by the current domain name
        private int _maxDeep = 5;//Search depth (referring to the search level of entering the URL through the page, not the actual path depth of the web page in the website)
        private int _maxPageCount = 500;//Maximum number of pages allowed to be included
        private List<UrlPathCollection> _urlPath;//path
        private int _requestPageCount = 0;//Number of pages requested
        private long _requestPageTicks = 0;//Request page time
        private int _threadInUsing = 0;//Number of threads currently used

        /// <summary>
        ///Number of threads in use
        /// </summary>
        private int threadInUsing
        {
            set
            {
                lock (syncLock)
                {
                    _threadInUsing = value;
                }
            }
            get
            {
                lock (syncLock)
                {
                    return _threadInUsing;
                }
            }
        }

        private object syncLock = new object();//Lock

        //private SenMapicSemaphore _semaphorePool;
        //private int _semaphorePoolPreviousCount;
        //private int semaphorePoolPreviousCount
        //{
        //    get
        //    {
        //        lock (syncLock)
        //        {
        //            return _semaphorePoolPreviousCount;
        //        }
        //    }
        //    set
        //    {
        //        lock (syncLock)
        //        {
        //            _semaphorePoolPreviousCount = value;
        //        }
        //    }
        //}

        private int _remainDomainCount;//Remaining unprocessed domain names
        int _remainUrlCount = 0;//Number of remaining uncollected urls

        public IEnumerable<string> TotalDomains { get { return _urls; } }
        public Dictionary<string, UrlData> TotalUrls { get; private set; }//Urls collected from all domain names
        public double TotalPageSizeKB { get; private set; }
        public DateTime? UpdateDate { get; set; }
        public string Priority { get; set; }
        public string Changefreq { get; set; }
        public DateTime CurrentSiteStartTime { get; private set; }//Current search site start time
        public int MaxBuildMinutesForSingleSite { get { return this._maxBuildMinutesForSingleSite; } set { this._maxBuildMinutesForSingleSite = value; } }//Maximum allowed time for a single site

        public bool ReachMaxPages { get; private set; }//Maximum number of pages reached
        public int MaxDeep { get { return _maxDeep; } set { _maxDeep = value; } }
        public int MaxPageCount { get { return _maxPageCount; } set { _maxPageCount = value; } }
        public TimeSpan AverageRequestTime { get { return TimeSpan.FromTicks(_requestPageCount == 0 ? 0 : (_requestPageTicks / _requestPageCount)); } }
        public List<FilterOmitWord> FilterOmitWords;//Filter keywords in Url
        //public Dictionary<string,Dictionary<string,UrlData>> TotalUrlDataCollection { get; set; }

      
        public SenMapicEngine(IServiceProvider serviceProvider, IEnumerable<string> urls, int maxThread=4, int maxBuildMinutesForSingleSite=10, string priority=null, string changefreq=null, DateTime? updateDate=null, int maxDeep=5, int maxPageCount=500, List<string> filterOmitWords=null)
        {
            this._serviceProvider = serviceProvider;
            if (changefreq != null && priority != "none")
            {
                Priority = priority;
            }
            if (changefreq != null && changefreq != "none")
            {
                Changefreq = changefreq;
            }
            if (updateDate != null)
            {
                UpdateDate = updateDate;
            }

            if (maxThread > 0 && maxThread <= 20)
            {
                _maxThread = maxThread;
            }

            if (maxDeep > 0)
            {
                MaxDeep = maxDeep;
            }

            if (maxPageCount > 0)
            {
                MaxPageCount = maxPageCount;
            }

            _currentAvaliableUrlTemp = new Dictionary<string, AvailableUrl>(StringComparer.OrdinalIgnoreCase);
            _currentUrls = new Dictionary<string, UrlData>(StringComparer.OrdinalIgnoreCase);
            _urlPath = new List<UrlPathCollection>();
            TotalUrls = new Dictionary<string, UrlData>(StringComparer.OrdinalIgnoreCase);
            FilterOmitWords = this.GetFilterOmitWords(filterOmitWords == null ? new List<string>() : filterOmitWords);
            //TotalUrlDataCollection = new Dictionary<string, Dictionary<string, UrlData>>();
            _urls = urls.Distinct();
            _remainDomainCount = _urls.Count();

            MaxBuildMinutesForSingleSite = maxBuildMinutesForSingleSite;
        }

        /// <summary>
        /// Start collecting Url and page information
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, UrlData> Build()
        {
            // System.Web.HttpContext.Current.Response.Write(this.GetDomain(url));
            //Search all URLs
            foreach (var url in _urls)
            {
                try
                {
                    DateTime dt1 = DateTime.Now;
                    _currentUrl = url;
                    if (!_currentUrl.EndsWith("/"))
                    {
                        _currentUrl += "/";//Add the trailing / (in order to obtain a more complete URL, so as to determine whether the subordinate link URL is equal to the domain name root directory)
                    }
                    _currentDomain = this.GetDomain(_currentUrl);

                    //_domainAll += (_domainAll == null ? "" : "+") + _currentDomain;//All domain names

                    if (string.IsNullOrEmpty(_currentDomain))
                    {
                        throw new Exception("请提供正确的网址！");
                    }
                    _currentUrlBuildStop = false;
                    _currentProtocol = this.GetProtocol(_currentUrl);
                    _currentOriginalFullDomain = this.GetFullDomain(_currentUrl);//Such as: http://www.senparc.com
                    _currentDeep = -1;//Deep restoration
                    _currentPageCount = 0;//Page count restoration
                    //_currentFilterOmitWords = new Dictionary<string, string>();//Filter keyword restoration
                    _currentAvaliableUrlTemp = new Dictionary<string, AvailableUrl>(StringComparer.OrdinalIgnoreCase);
                    _currentUrls = new Dictionary<string, UrlData>(StringComparer.OrdinalIgnoreCase);//Current domain name collection URL restoration
                    ResetUrlPath(_currentUrl);//Initialize the UrlCollection collection
                    CurrentSiteStartTime = DateTime.Now;

                    //_semaphorePool = new SenMapicSemaphore(_maxThread);//Use Semaphore to control the current number of threads

                    this.SearchWebSite(_currentUrl);//Start collection (core method)
                    //TotalUrlDataCollection.Add(_currentUrl, _currentUrls);//Add UrlData results to the summary collection

                    foreach (var item in _currentUrls.Take(MaxPageCount).ToList()/*Make sure the number of included URLs is correct (due to deep reasons, the actual number of included URLs may be more than the set value)*/)
                    {
                        if (!TotalUrls.ContainsKey(item.Key))
                        {
                            TotalUrls.Add(item.Key, item.Value);
                        }
                    }

                    DateTime dt2 = DateTime.Now;
                    TimeSpan ts = dt2 - dt1;
                    //write log
                    SenparcTrace.SendCustomLog("SiteMap", $"Sitemap生成：{this._currentUrl}，收录页面：{this._currentUrls.Count}，总耗时：{ts.TotalMilliseconds:###,###}毫秒，平均{(this._currentUrls.Count > 0 ? (ts.TotalMilliseconds / this._currentUrls.Count).ToString("###,###") : "0")}毫秒。");
                }
                catch (Exception e)
                {
                    var ex = new Exception($"Sitemap生成出错。url:{this._currentUrl}", e);
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }
                finally
                {
                    _remainDomainCount--;//The number of remaining unprocessed domain names decreases
                }
            }

            //ReachMaxPages = TotalUrls.Count >= this._maxPageCount;


            //BuildSiteMapXML();
            //return root;

            return TotalUrls;

            //}
            //catch(Exception e) {
            //    LogUtility.SitemapLogger.Error("Error in generating report and sitemap file: {0}".With(e.Message), e);
            //    return null;
            //}
        }


        private static ManualResetEvent allDoneSL = new ManualResetEvent(false);

        /// <summary>
        /// Crawl site (SenMapic core startup method)
        /// </summary>
        /// <param name="homeUrl">Domain name or homepage address</param>
        private void SearchWebSite(string homeUrl)
        {
            homeUrl = this.RemoveUrlEndingSlash(homeUrl);
            this._currentAvaliableUrlTemp.Add(homeUrl, new AvailableUrl(homeUrl, "", 0, "",AvailableUrlStatus.UnStart));//Home page added to be selected

            WaitCallback waitCallback = new WaitCallback(async (url) => await this.CrawlUrlEventHandler(url));//WaitCallback

            //Start crawling from layer 0 (home page)
            for (int deep = 0; deep <= this._maxDeep; deep++)
            {
                if (threadInUsing > 0)
                {
                    //Before the end of each layer, it will wait for all threads to end. threadInUsing > 0 means there are still threads working at this time.
                    SenparcTrace.SendCustomLog("Sitemap", $"Sitemap多线程出错！当前实际线程数：{threadInUsing},Deep:{deep},Url:{homeUrl}");
                }

                if (this._currentUrls.Count >= this._maxPageCount || this._currentUrlBuildStop)
                {
                    break;//The collection is full, or was called to stop.
                }

                _currentDeep = deep;
                //_urlPath[_currentDeep].Url = url;//Record the current level Url
                var thisLayerUrls = this._currentAvaliableUrlTemp.Where(z => z.Value.Deep == deep && z.Value.Status == AvailableUrlStatus.UnStart).Select(z => z.Value.Url).ToList();//All valid URLs in the current layer are the Url to start collecting.
                if (thisLayerUrls.Count == 0)
                {
                    break;//All pages are completed.
                }

                //Start recording in separate threads and add to thread pool
                _remainUrlCount = 0;
                int urlIndex = 0;
                foreach (var url in thisLayerUrls)
                {
                    if (this._currentUrls.Count >= this._maxPageCount || this._currentUrlBuildStop)
                    {
                        break;//The collection is full, or was called to stop.
                    }

                    #region 多线程爬行方案
                    try
                    {
                        int tryQueue = 0;
                        while (tryQueue < 3)//Tried 3 times to join the queue
                        {
                            bool queueSuccess = ThreadPool.QueueUserWorkItem(waitCallback, url);//Try to join the queue
                            if (queueSuccess)
                            {
                                //_remainUrlCount++;
                                threadInUsing++;
                                break;
                            }
                            else
                            {
                                tryQueue++;//try to queue
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var ex = new Exception($"Sitemap多线程出错！当前实际线程数：{threadInUsing},Deep:{deep},Url:{homeUrl}", e);
                        SenparcTrace.BaseExceptionLog(ex);
                    }

                    #region 等待线程，依次执行
                    //Here, if all threads are added to the queue at once, when waiting using _semaphorePool.WaitOne(),
                    //When the number of links in a certain layer is very large (such as hundreds), the entire application will become unresponsive.

                    double waitMinutesPerUrl = 1;
                    double totalWaitMinutes = _maxThread * waitMinutesPerUrl;

                    DateTime dtStartWait = DateTime.Now;
                    bool firstWait = true;
                    while (threadInUsing >= _maxThread || (threadInUsing > 0 && urlIndex == thisLayerUrls.Count - 1) /*|| _remainUrlCount > 0*/ || firstWait)//At the end of each layer, ensure that no threads are occupied
                    {
                        if (firstWait)
                        {
                            firstWait = false;
                        }

                        if ((DateTime.Now - dtStartWait).TotalMinutes > totalWaitMinutes)/*Page collection for each thread takes more than 0.5 minutes and forced exit*/
                        {
                            var ex = new Exception($"Sitemap线程超时，强制忽略。首页:{homeUrl}，已等待{totalWaitMinutes}分钟，当前使用线程数：{threadInUsing}，最大允许线程数：{_maxThread}，当前层Url数：{thisLayerUrls.Count}，当前使用线程数：{threadInUsing}，当前剩余Url数：{_remainUrlCount}");
                            SenparcTrace.BaseExceptionLog(ex);
                            break;
                        }

                        Thread.Sleep(200);//There are still threads that have not finished executing
                    }
                    #endregion
                    #endregion

                    #region 单线程爬行方案
                    //CrawlUrlEventHandler(url);//Do not use multi-threading, single thread
                    #endregion

                    urlIndex++;
                }

                //After searching in all link libraries, start the next level.
                Thread.Sleep(500);//take a break:)
            }
        }

        /// <summary>
        /// Provide multi-threaded simultaneous recording
        /// </summary>
        /// <param name="url">Full URL, in the format: http://www.senparc.com/xx.html</param>
        private async Task CrawlUrlEventHandler(object url)
        {
            //bool threadStarted = false;
            string synDataID = Guid.NewGuid().ToString();
            try
            {
                _remainUrlCount++;
                //LogUtility.WebLogger.DebugFormat("url:{0} has entered the queue and is waiting to start.", url);
                //_semaphorePool.WaitOne();//Waiting to join the semaphore queue

                //threadStarted = true;
                //threadInUsing++;
                //LogUtility.WebLogger.DebugFormat("url:{0} thread officially started. Current number of running threads: {1}", url, threadInUsing);

                string avaliableUrl = url as string;
                AvailableUrl currentAvaliableUrl = this._currentAvaliableUrlTemp[avaliableUrl];

                string parentUrl = currentAvaliableUrl.ParentUrl;

                try
                {
                    SenMapicSynData synData = SenMapicSynData.Instance;
                    synData.CurrentCrawlingUrlList[synDataID] = currentAvaliableUrl;//Add global synchronization information
                }
                catch (Exception e)
                {
                    var ex = new Exception($"CurrentCrawlingUrlList出错，Url:{url}", e);
                    SenparcTrace.BaseExceptionLog(ex);
                }
                UrlData urlData;
                try
                {
                    currentAvaliableUrl.StartBuildTime = DateTime.Now;
                    urlData = await this.CrawlUrl(avaliableUrl, parentUrl, _currentDeep, false, currentAvaliableUrl.LinkText);//Crawl Url and obtain Url, HTML and other important information
                }
                catch (Exception e)
                {
                    var ex = new Exception($"Sitemap出错！当前Url:{avaliableUrl}，ParentUrl:{parentUrl}", e);
                    SenparcTrace.BaseExceptionLog(ex);
                    urlData = new UrlData(avaliableUrl, -1, "", "", -1, 0.00, parentUrl, currentAvaliableUrl.LinkText);
                }

                this._currentAvaliableUrlTemp[avaliableUrl].Status = AvailableUrlStatus.Finished;//Crawl completed
                if (urlData == null /*|| urlData.Html.IsNullOrEmpty()*/)
                {
                    return; //Not eligible for crawling
                }

                urlData.ParentUrl = parentUrl;//Parent page Url

                if (!this._currentUrls.ContainsKey(urlData.Url))
                {
                    this._currentUrls.Add(urlData.Url.ToUpper()/*Key capitalized*/, urlData);//Record in completed table.
                }

                if (!urlData.Url.IsNullOrEmpty() && urlData.Url != avaliableUrl)
                {
                    avaliableUrl = urlData.Url;//The URL has been redirected and the availableUrl needs to be updated.
                }

                #region 分析链接（收集有效Url，未开始抓取）
                if (_currentDeep < this._maxDeep)
                {
                    //Before reaching the last level, collect all available web pages in the next level of the changed page.
                    MatchCollection aTags = Regex.Matches(urlData.Html, urlPattern);
                    //TODO: Distinct aTags
                    foreach (Match tag in aTags)
                    {
                        string newUrl = tag.Groups[2].Value; // Get href attribute value  
                        string linkText = tag.Groups[4].Value; // Get hyperlink text 
                        newUrl = GetFullUrl(newUrl, avaliableUrl);
                        if (!newUrl.IsNullOrEmpty())
                        {
                            //Save to valid link library
                            if (!this._currentAvaliableUrlTemp.ContainsKey(newUrl))
                            {
                                this._currentAvaliableUrlTemp.Add(newUrl, new AvailableUrl(newUrl, urlData.Url/*CurrentUrl*/, _currentDeep + 1/*Belongs to the next level*/, linkText/*hyperlink text*/, AvailableUrlStatus.UnStart));//Marking not started
                            }
                        }
                        else
                        {
                            //continue;//Invalid Url, or out of range
                        }
                    }
                }
                #endregion
            }
            catch (Exception e)
            {
                var ex = new Exception($"Url:{url}。出错信息：{e.Message}", e);
                SenparcTrace.BaseExceptionLog(ex);
            }
            finally
            {
                //_semaphorePool.Release();
                //LogUtility.WebLogger.DebugFormat("url:{0} thread exited.", url);

                //if (threadStarted)
                {
                    threadInUsing--;
                }
                _remainUrlCount--;

                SenMapicSynData synData = SenMapicSynData.Instance;
                synData.CurrentCrawlingUrlList.Remove(synDataID);
                //allDoneSL.Set();
                //Thread.Sleep(80);//Take a rest :)
            }
        }
    }
}