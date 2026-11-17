/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicEngine.cs
    File description: SenMapicEngine related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
        private static string _version = "1.7.0.0";//Legacy comment translated to English.
        /// <summary>
        /// SenMapicEngineversion
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
        private const string urlPattern = "<a[^>]+href=(['\"]{0,1})([^'\"#>\\s\\(\\)]+)(?=['\"#>]{1}).*?>(.*?)</a>";//Legacy comment translated to English.
        private int _maxThread = 2;//Legacy comment translated to English.
        private readonly int _maxTimeoutTimes = 10;//Legacy comment translated to English.
        private readonly int _pageRequestTimeoutMillionSeconds = 1000 * 20;//Legacy comment translated to English.
        private readonly IServiceProvider _serviceProvider;
        private int _maxBuildMinutesForSingleSite = 10;//Legacy comment translated to English.

        private IEnumerable<string> _urls;//Legacy comment translated to English.
        private string _currentUrl;//Legacy comment translated to English.
        private bool _currentUrlBuildStop;//Legacy comment translated to English.
        private string _currentOriginalFullDomain;//Legacy comment translated to English.
        private string _currentDomain;//Legacy comment translated to English.
        private string _currentProtocol;//Legacy comment translated to English.
        private int _currentDeep;//Legacy comment translated to English.
        private int _currentPageCount;//Legacy comment translated to English.
        private Dictionary<string, AvailableUrl> _currentAvaliableUrlTemp;//Legacy comment translated to English.
        private Dictionary<string, UrlData> _currentUrls;//Legacy comment translated to English.
        private int _maxDeep = 5;//Legacy comment translated to English.
        private int _maxPageCount = 500;//Legacy comment translated to English.
        private List<UrlPathCollection> _urlPath;//path
        private int _requestPageCount = 0;//Legacy comment translated to English.
        private long _requestPageTicks = 0;//Legacy comment translated to English.
        private int _threadInUsing = 0;//Legacy comment translated to English.

        /// <summary>
        ///Legacy comment translated to English.
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

        private object syncLock = new object();//Legacy comment translated to English.

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

        private int _remainDomainCount;//Legacy comment translated to English.
        int _remainUrlCount = 0;//Legacy comment translated to English.

        public IEnumerable<string> TotalDomains { get { return _urls; } }
        public Dictionary<string, UrlData> TotalUrls { get; private set; }//Legacy comment translated to English.
        public double TotalPageSizeKB { get; private set; }
        public DateTime? UpdateDate { get; set; }
        public string Priority { get; set; }
        public string Changefreq { get; set; }
        public DateTime CurrentSiteStartTime { get; private set; }//Legacy comment translated to English.
        public int MaxBuildMinutesForSingleSite { get { return this._maxBuildMinutesForSingleSite; } set { this._maxBuildMinutesForSingleSite = value; } }//Legacy comment translated to English.

        public bool ReachMaxPages { get; private set; }//Legacy comment translated to English.
        public int MaxDeep { get { return _maxDeep; } set { _maxDeep = value; } }
        public int MaxPageCount { get { return _maxPageCount; } set { _maxPageCount = value; } }
        public TimeSpan AverageRequestTime { get { return TimeSpan.FromTicks(_requestPageCount == 0 ? 0 : (_requestPageTicks / _requestPageCount)); } }
        public List<FilterOmitWord> FilterOmitWords;//Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, UrlData> Build()
        {
            // System.Web.HttpContext.Current.Response.Write(this.GetDomain(url));
            //Legacy comment translated to English.
            foreach (var url in _urls)
            {
                try
                {
                    DateTime dt1 = DateTime.Now;
                    _currentUrl = url;
                    if (!_currentUrl.EndsWith("/"))
                    {
                        _currentUrl += "/";//Legacy comment translated to English.
                    }
                    _currentDomain = this.GetDomain(_currentUrl);

                    //Legacy comment translated to English.

                    if (string.IsNullOrEmpty(_currentDomain))
                    {
                        throw new Exception("Please provide a valid URL.");
                    }
                    _currentUrlBuildStop = false;
                    _currentProtocol = this.GetProtocol(_currentUrl);
                    _currentOriginalFullDomain = this.GetFullDomain(_currentUrl);//Legacy comment translated to English.
                    _currentDeep = -1;//Legacy comment translated to English.
                    _currentPageCount = 0;//Legacy comment translated to English.
                    //Legacy comment translated to English.
                    _currentAvaliableUrlTemp = new Dictionary<string, AvailableUrl>(StringComparer.OrdinalIgnoreCase);
                    _currentUrls = new Dictionary<string, UrlData>(StringComparer.OrdinalIgnoreCase);//Legacy comment translated to English.
                    ResetUrlPath(_currentUrl);//Legacy comment translated to English.
                    CurrentSiteStartTime = DateTime.Now;

                    //Legacy comment translated to English.

                    this.SearchWebSite(_currentUrl);//startcollect（coremethod）
                    //Legacy comment translated to English.

                    foreach (var item in _currentUrls.Take(MaxPageCount).ToList()/*Legacy comment translated to English.*/)
                    {
                        if (!TotalUrls.ContainsKey(item.Key))
                        {
                            TotalUrls.Add(item.Key, item.Value);
                        }
                    }

                    DateTime dt2 = DateTime.Now;
                    TimeSpan ts = dt2 - dt1;
                    //Legacy comment translated to English.
                    SenparcTrace.SendCustomLog("SiteMap", $"Sitemap generated: {this._currentUrl}; pages collected: {this._currentUrls.Count}; total time: {ts.TotalMilliseconds:###,###} ms; average: {(this._currentUrls.Count > 0 ? (ts.TotalMilliseconds / this._currentUrls.Count).ToString("###,###") : "0")} ms.");
                }
                catch (Exception e)
                {
                    var ex = new Exception($"Sitemap generation failed. URL: {this._currentUrl}", e);
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }
                finally
                {
                    _remainDomainCount--;//Legacy comment translated to English.
                }
            }

            //ReachMaxPages = TotalUrls.Count >= this._maxPageCount;


            //BuildSiteMapXML();
            //return root;

            return TotalUrls;

            //}
            //catch(Exception e) {
            //Legacy comment translated to English.
            //    return null;
            //}
        }


        private static ManualResetEvent allDoneSL = new ManualResetEvent(false);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        private void SearchWebSite(string homeUrl)
        {
            homeUrl = this.RemoveUrlEndingSlash(homeUrl);
            this._currentAvaliableUrlTemp.Add(homeUrl, new AvailableUrl(homeUrl, "", 0, "",AvailableUrlStatus.UnStart));//Legacy comment translated to English.

            WaitCallback waitCallback = new WaitCallback(async (url) => await this.CrawlUrlEventHandler(url));//WaitCallback

            //Legacy comment translated to English.
            for (int deep = 0; deep <= this._maxDeep; deep++)
            {
                if (threadInUsing > 0)
                {
                    //Legacy comment translated to English.
                        SenparcTrace.SendCustomLog("Sitemap", $"Sitemap multithreading failed. Active threads: {threadInUsing}, depth: {deep}, URL: {homeUrl}");
                }

                if (this._currentUrls.Count >= this._maxPageCount || this._currentUrlBuildStop)
                {
                    break;//Legacy comment translated to English.
                }

                _currentDeep = deep;
                //Legacy comment translated to English.
                var thisLayerUrls = this._currentAvaliableUrlTemp.Where(z => z.Value.Deep == deep && z.Value.Status == AvailableUrlStatus.UnStart).Select(z => z.Value.Url).ToList();//Legacy comment translated to English.
                if (thisLayerUrls.Count == 0)
                {
                    break;//Legacy comment translated to English.
                }

                //Legacy comment translated to English.
                _remainUrlCount = 0;
                int urlIndex = 0;
                foreach (var url in thisLayerUrls)
                {
                    if (this._currentUrls.Count >= this._maxPageCount || this._currentUrlBuildStop)
                    {
                        break;//Legacy comment translated to English.
                    }

                    #region Legacy section
                    try
                    {
                        int tryQueue = 0;
                        while (tryQueue < 3)//Legacy comment translated to English.
                        {
                            bool queueSuccess = ThreadPool.QueueUserWorkItem(waitCallback, url);//Legacy comment translated to English.
                            if (queueSuccess)
                            {
                                //_remainUrlCount++;
                                threadInUsing++;
                                break;
                            }
                            else
                            {
                                tryQueue++;//Legacy comment translated to English.
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var ex = new Exception($"Sitemap multithreading failed. Active threads: {threadInUsing}, depth: {deep}, URL: {homeUrl}", e);
                        SenparcTrace.BaseExceptionLog(ex);
                    }

                    #region Legacy section
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.

                    double waitMinutesPerUrl = 1;
                    double totalWaitMinutes = _maxThread * waitMinutesPerUrl;

                    DateTime dtStartWait = DateTime.Now;
                    bool firstWait = true;
                    while (threadInUsing >= _maxThread || (threadInUsing > 0 && urlIndex == thisLayerUrls.Count - 1) /*|| _remainUrlCount > 0*/ || firstWait)//Legacy comment translated to English.
                    {
                        if (firstWait)
                        {
                            firstWait = false;
                        }

                        if ((DateTime.Now - dtStartWait).TotalMinutes > totalWaitMinutes)/*Legacy comment translated to English.*/
                        {
                            var ex = new Exception($"Sitemap thread timed out and was skipped. Home URL: {homeUrl}; waited: {totalWaitMinutes} minutes; active threads: {threadInUsing}; maximum threads: {_maxThread}; URLs at this level: {thisLayerUrls.Count}; remaining URLs: {_remainUrlCount}");
                            SenparcTrace.BaseExceptionLog(ex);
                            break;
                        }

                        Thread.Sleep(200);//Legacy comment translated to English.
                    }
                    #endregion
                    #endregion

                    #region Legacy section
                    //Legacy comment translated to English.
                    #endregion

                    urlIndex++;
                }

                //Legacy comment translated to English.
                Thread.Sleep(500);//Legacy comment translated to English.
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        private async Task CrawlUrlEventHandler(object url)
        {
            //bool threadStarted = false;
            string synDataID = Guid.NewGuid().ToString();
            try
            {
                _remainUrlCount++;
                //Legacy comment translated to English.
                //Legacy comment translated to English.

                //threadStarted = true;
                //threadInUsing++;
                //Legacy comment translated to English.

                string avaliableUrl = url as string;
                AvailableUrl currentAvaliableUrl = this._currentAvaliableUrlTemp[avaliableUrl];

                string parentUrl = currentAvaliableUrl.ParentUrl;

                try
                {
                    SenMapicSynData synData = SenMapicSynData.Instance;
                    synData.CurrentCrawlingUrlList[synDataID] = currentAvaliableUrl;//Legacy comment translated to English.
                }
                catch (Exception e)
                {
                    var ex = new Exception($"CurrentCrawlingUrlList failed. URL: {url}", e);
                    SenparcTrace.BaseExceptionLog(ex);
                }
                UrlData urlData;
                try
                {
                    currentAvaliableUrl.StartBuildTime = DateTime.Now;
                    urlData = await this.CrawlUrl(avaliableUrl, parentUrl, _currentDeep, false, currentAvaliableUrl.LinkText);//Legacy comment translated to English.
                }
                catch (Exception e)
                {
                    var ex = new Exception($"Sitemap failed. Current URL: {avaliableUrl}; parent URL: {parentUrl}", e);
                    SenparcTrace.BaseExceptionLog(ex);
                    urlData = new UrlData(avaliableUrl, -1, "", "", -1, 0.00, parentUrl, currentAvaliableUrl.LinkText);
                }

                this._currentAvaliableUrlTemp[avaliableUrl].Status = AvailableUrlStatus.Finished;//Legacy comment translated to English.
                if (urlData == null /*|| urlData.Html.IsNullOrEmpty()*/)
                {
                    return; //Legacy comment translated to English.
                }

                urlData.ParentUrl = parentUrl;//Legacy comment translated to English.

                if (!this._currentUrls.ContainsKey(urlData.Url))
                {
                    this._currentUrls.Add(urlData.Url.ToUpper()/*Legacy comment translated to English.*/, urlData);//Legacy comment translated to English.
                }

                if (!urlData.Url.IsNullOrEmpty() && urlData.Url != avaliableUrl)
                {
                    avaliableUrl = urlData.Url;//Legacy comment translated to English.
                }

                #region Legacy section
                if (_currentDeep < this._maxDeep)
                {
                    //Legacy comment translated to English.
                    MatchCollection aTags = Regex.Matches(urlData.Html, urlPattern);
                    //TODO: Distinct aTags
                    foreach (Match tag in aTags)
                    {
                        string newUrl = tag.Groups[2].Value; //Legacy comment translated to English.
                        string linkText = tag.Groups[4].Value; //Legacy comment translated to English.
                        newUrl = GetFullUrl(newUrl, avaliableUrl);
                        if (!newUrl.IsNullOrEmpty())
                        {
                            //Legacy comment translated to English.
                            if (!this._currentAvaliableUrlTemp.ContainsKey(newUrl))
                            {
                                this._currentAvaliableUrlTemp.Add(newUrl, new AvailableUrl(newUrl, urlData.Url/*currentUrl*/, _currentDeep + 1/*Legacy comment translated to English.*/, linkText/*Legacy comment translated to English.*/, AvailableUrlStatus.UnStart));//Legacy comment translated to English.
                            }
                        }
                        else
                        {
                            //Legacy comment translated to English.
                        }
                    }
                }
                #endregion
            }
            catch (Exception e)
            {
                var ex = new Exception($"URL: {url}. Error: {e.Message}", e);
                SenparcTrace.BaseExceptionLog(ex);
            }
            finally
            {
                //_semaphorePool.Release();
                //Legacy comment translated to English.

                //if (threadStarted)
                {
                    threadInUsing--;
                }
                _remainUrlCount--;

                SenMapicSynData synData = SenMapicSynData.Instance;
                synData.CurrentCrawlingUrlList.Remove(synDataID);
                //allDoneSL.Set();
                //Legacy comment translated to English.
            }
        }
    }
}
