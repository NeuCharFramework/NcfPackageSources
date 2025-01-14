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
        private static string _version = "1.7.0.0";//TODO:目前为Beta 2
        /// <summary>
        /// SenMapicEngine版本
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
        private const string urlPattern = "(?<=<a[^>]+href=['\"]{0,1})([^'\"#>\\s\\(\\)]+)(?=['\"#>]{1})";//TODO:过滤js中此类情形："<a href=\""+ url +"\">";Joomla中，url有;出现
        private int _maxThread = 2;//最大线程数
        private readonly int _maxTimeoutTimes = 10;//最大超时页面数（超过此数量，终止收集）
        private readonly int _pageRequestTimeoutMillionSeconds = 1000 * 20;//页面请求超时时间（毫秒）
        private readonly IServiceProvider _serviceProvider;
        private int _maxBuildMinutesForSingleSite = 10;//默认最长收集时间（每一个站点）

        private IEnumerable<string> _urls;//带收录的列队Url
        private string _currentUrl;//当前搜索的Url
        private bool _currentUrlBuildStop;//停止当前站点搜索
        private string _currentOriginalFullDomain;//当前原始完整的域名（http://www.senparc.com）
        private string _currentDomain;//当前搜索的域名
        private string _currentProtocol;//协议（http | https）
        private int _currentDeep;//当前收录深度
        private int _currentPageCount;//当前域名下收录页面数量
        private Dictionary<string, AvailableUrl> _currentAvaliableUrlTemp;//当前可用Url集合   TODO:lock
        private Dictionary<string, UrlData> _currentUrls;//当前域域名收集的所有URL
        private int _maxDeep = 5;//搜索深度（指通过页面进入网址的搜索层次，非网页在网站中的实际路径深度）
        private int _maxPageCount = 500;//最大允许收录页面数
        private List<UrlPathCollection> _urlPath;//路径
        private int _requestPageCount = 0;//请求页面数量
        private long _requestPageTicks = 0;//请求页面时间
        private int _threadInUsing = 0;//当前使用线程数

        /// <summary>
        /// 正在使用中的线程数
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

        private object syncLock = new object();//锁

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

        private int _remainDomainCount;//剩余未处理域名
        int _remainUrlCount = 0;//剩余未收录url数量

        public IEnumerable<string> TotalDomains { get { return _urls; } }
        public Dictionary<string, UrlData> TotalUrls { get; private set; }//所有域名收集的Url
        public double TotalPageSizeKB { get; private set; }
        public DateTime? UpdateDate { get; set; }
        public string Priority { get; set; }
        public string Changefreq { get; set; }
        public DateTime CurrentSiteStartTime { get; private set; }//当前搜索站点开始时间
        public int MaxBuildMinutesForSingleSite { get { return this._maxBuildMinutesForSingleSite; } set { this._maxBuildMinutesForSingleSite = value; } }//单个站点对大允许时间

        public bool ReachMaxPages { get; private set; }//到达最大页面数
        public int MaxDeep { get { return _maxDeep; } set { _maxDeep = value; } }
        public int MaxPageCount { get { return _maxPageCount; } set { _maxPageCount = value; } }
        public TimeSpan AverageRequestTime { get { return TimeSpan.FromTicks(_requestPageCount == 0 ? 0 : (_requestPageTicks / _requestPageCount)); } }
        public List<FilterOmitWord> FilterOmitWords;//过滤Url中的关键字
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
        /// 开始收集Url及页面信息
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, UrlData> Build()
        {
            // System.Web.HttpContext.Current.Response.Write(this.GetDomain(url));
            //搜索所有网址
            foreach (var url in _urls)
            {
                try
                {
                    DateTime dt1 = DateTime.Now;
                    _currentUrl = url;
                    if (!_currentUrl.EndsWith("/"))
                    {
                        _currentUrl += "/";//加上末尾/（为了获得更完整的URL，以便判断下属链接URL是否等于域名根目录）
                    }
                    _currentDomain = this.GetDomain(_currentUrl);

                    //_domainAll += (_domainAll == null ? "" : "+") + _currentDomain;//所有域名

                    if (string.IsNullOrEmpty(_currentDomain))
                    {
                        throw new Exception("请提供正确的网址！");
                    }
                    _currentUrlBuildStop = false;
                    _currentProtocol = this.GetProtocol(_currentUrl);
                    _currentOriginalFullDomain = this.GetFullDomain(_currentUrl);//如：http://www.senparc.com
                    _currentDeep = -1;//深度还原
                    _currentPageCount = 0;//页面计数还原
                    //_currentFilterOmitWords = new Dictionary<string, string>();//过滤关键字还原
                    _currentAvaliableUrlTemp = new Dictionary<string, AvailableUrl>(StringComparer.OrdinalIgnoreCase);
                    _currentUrls = new Dictionary<string, UrlData>(StringComparer.OrdinalIgnoreCase);//当前域名收集URL还原
                    ResetUrlPath(_currentUrl);//初始化UrlCollection集合
                    CurrentSiteStartTime = DateTime.Now;

                    //_semaphorePool = new SenMapicSemaphore(_maxThread);//使用Semaphore控制当前线程数量

                    this.SearchWebSite(_currentUrl);//开始收集（核心方法）
                    //TotalUrlDataCollection.Add(_currentUrl, _currentUrls);//添加UrlData结果到汇总集合

                    foreach (var item in _currentUrls.Take(MaxPageCount).ToList()/*确保收录数量正确（由于deep的原因，可能导致实际收录url数量比设定值要多）*/)
                    {
                        if (!TotalUrls.ContainsKey(item.Key))
                        {
                            TotalUrls.Add(item.Key, item.Value);
                        }
                    }

                    DateTime dt2 = DateTime.Now;
                    TimeSpan ts = dt2 - dt1;
                    //写入日志
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
                    _remainDomainCount--;//剩余未处理域名数减少
                }
            }

            //ReachMaxPages = TotalUrls.Count >= this._maxPageCount;


            //BuildSiteMapXML();
            //return root;

            return TotalUrls;

            //}
            //catch(Exception e) {
            //    LogUtility.SitemapLogger.Error("生成报表及sitemap文件出错：{0}".With(e.Message), e);
            //    return null;
            //}
        }


        private static ManualResetEvent allDoneSL = new ManualResetEvent(false);

        /// <summary>
        /// 爬行站点（SenMapic核心启动方法）
        /// </summary>
        /// <param name="homeUrl">域名或首页地址</param>
        private void SearchWebSite(string homeUrl)
        {
            homeUrl = this.RemoveUrlEndingSlash(homeUrl);
            this._currentAvaliableUrlTemp.Add(homeUrl, new AvailableUrl(homeUrl, "", 0, AvailableUrlStatus.UnStart));//首页加入待选

            WaitCallback waitCallback = new WaitCallback(async (url) => await this.CrawlUrlEventHandler(url));//WaitCallback

            //开始从第0层（首页）抓取
            for (int deep = 0; deep <= this._maxDeep; deep++)
            {
                if (threadInUsing > 0)
                {
                    //每层结束前，会等待所有线程结束，threadInUsing > 0 说明此时还有线程在工作
                    SenparcTrace.SendCustomLog("Sitemap", $"Sitemap多线程出错！当前实际线程数：{threadInUsing},Deep:{deep},Url:{homeUrl}");
                }

                if (this._currentUrls.Count >= this._maxPageCount || this._currentUrlBuildStop)
                {
                    break;//收集已满，或被呼叫停止。
                }

                _currentDeep = deep;
                //_urlPath[_currentDeep].Url = url;//记录当前层次Url
                var thisLayerUrls = this._currentAvaliableUrlTemp.Where(z => z.Value.Deep == deep && z.Value.Status == AvailableUrlStatus.UnStart).Select(z => z.Value.Url).ToList();//当前层所有有效，且为开始收集的Url
                if (thisLayerUrls.Count == 0)
                {
                    break;//所有页面已完成。
                }

                //分线程开始爪录，添加到线程池
                _remainUrlCount = 0;
                int urlIndex = 0;
                foreach (var url in thisLayerUrls)
                {
                    if (this._currentUrls.Count >= this._maxPageCount || this._currentUrlBuildStop)
                    {
                        break;//收集已满，或被呼叫停止。
                    }

                    #region 多线程爬行方案
                    try
                    {
                        int tryQueue = 0;
                        while (tryQueue < 3)//尝试3次加入列队
                        {
                            bool queueSuccess = ThreadPool.QueueUserWorkItem(waitCallback, url);//尝试加入列队
                            if (queueSuccess)
                            {
                                //_remainUrlCount++;
                                threadInUsing++;
                                break;
                            }
                            else
                            {
                                tryQueue++;//尝试放入列队
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        var ex = new Exception($"Sitemap多线程出错！当前实际线程数：{threadInUsing},Deep:{deep},Url:{homeUrl}", e);
                        SenparcTrace.BaseExceptionLog(ex);
                    }

                    #region 等待线程，依次执行
                    //这里如果一次全部将线程加入到列队，使用_semaphorePool.WaitOne()等待时，
                    //当某一层链接数量非常多（如几百个）时，会使整个应用程序无响应。

                    double waitMinutesPerUrl = 1;
                    double totalWaitMinutes = _maxThread * waitMinutesPerUrl;

                    DateTime dtStartWait = DateTime.Now;
                    bool firstWait = true;
                    while (threadInUsing >= _maxThread || (threadInUsing > 0 && urlIndex == thisLayerUrls.Count - 1) /*|| _remainUrlCount > 0*/ || firstWait)//每一层结束的时候确保没有线程被占用
                    {
                        if (firstWait)
                        {
                            firstWait = false;
                        }

                        if ((DateTime.Now - dtStartWait).TotalMinutes > totalWaitMinutes)/*每个线程页面采集大于大于0.5分钟，强制退出*/
                        {
                            var ex = new Exception($"Sitemap线程超时，强制忽略。首页:{homeUrl}，已等待{totalWaitMinutes}分钟，当前使用线程数：{threadInUsing}，最大允许线程数：{_maxThread}，当前层Url数：{thisLayerUrls.Count}，当前使用线程数：{threadInUsing}，当前剩余Url数：{_remainUrlCount}");
                            SenparcTrace.BaseExceptionLog(ex);
                            break;
                        }

                        Thread.Sleep(200);//还有线程没有执行完
                    }
                    #endregion
                    #endregion


                    #region 单线程爬行方案
                    //CrawlUrlEventHandler(url);//不使用多线程，单线程
                    #endregion

                    urlIndex++;
                }

                //所有链接库中搜索完毕之后，开始下一层。
                Thread.Sleep(500);//休息一下:)
            }
        }

        /// <summary>
        /// 提供多线程同时爪录
        /// </summary>
        /// <param name="url">完整URL，格式如：http://www.senparc.com/xx.html</param>
        private async Task CrawlUrlEventHandler(object url)
        {
            //bool threadStarted = false;
            string synDataID = Guid.NewGuid().ToString();
            try
            {
                _remainUrlCount++;
                //LogUtility.WebLogger.DebugFormat("url:{0} 进入列队等待开始。", url);
                //_semaphorePool.WaitOne();//等待加入semaphore列队

                //threadStarted = true;
                //threadInUsing++;
                //LogUtility.WebLogger.DebugFormat("url:{0} 线程正式开始。当前运行线程数：{1}", url, threadInUsing);

                string avaliableUrl = url as string;
                AvailableUrl currentAvaliableUrl = this._currentAvaliableUrlTemp[avaliableUrl];

                string parentUrl = currentAvaliableUrl.ParentUrl;

                try
                {
                    SenMapicSynData synData = SenMapicSynData.Instance;
                    synData.CurrentCrawlingUrlList[synDataID] = currentAvaliableUrl;//加入全局同步信息
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
                    urlData = await this.CrawlUrl(avaliableUrl, parentUrl, _currentDeep, false);//爬行Url,获取Url,HTML等重要信息
                }
                catch (Exception e)
                {
                    var ex = new Exception($"Sitemap出错！当前Url:{avaliableUrl}，ParentUrl:{parentUrl}", e);
                    SenparcTrace.BaseExceptionLog(ex);
                    urlData = new UrlData(avaliableUrl, -1, "", "", -1, 0.00, parentUrl);
                }

                this._currentAvaliableUrlTemp[avaliableUrl].Status = AvailableUrlStatus.Finished;//爬行完成
                if (urlData == null /*|| urlData.Html.IsNullOrEmpty()*/)
                {
                    return; //不符合爬行条件
                }

                urlData.ParentUrl = parentUrl;//父页面Url

                if (!this._currentUrls.ContainsKey(urlData.Url))
                {
                    this._currentUrls.Add(urlData.Url.ToUpper()/*Key大写*/, urlData);//记录到已完成表中。
                }

                if (!urlData.Url.IsNullOrEmpty() && urlData.Url != avaliableUrl)
                {
                    avaliableUrl = urlData.Url;//Url已经经过跳转，需要更新avaliableUrl
                }

                #region 分析链接（收集有效Url，未开始抓取）
                if (_currentDeep < this._maxDeep)
                {
                    //未到最后一层，收集改页面面下一层的所有可用网页
                    MatchCollection aTags = Regex.Matches(urlData.Html, urlPattern);
                    //TODO: Distinct aTags
                    foreach (Match tag in aTags)
                    {
                        string newUrl = tag.Value;
                        newUrl = GetFullUrl(newUrl, avaliableUrl);
                        if (!newUrl.IsNullOrEmpty())
                        {
                            //存入有效链接库
                            if (!this._currentAvaliableUrlTemp.ContainsKey(newUrl))
                            {
                                this._currentAvaliableUrlTemp.Add(newUrl, new AvailableUrl(newUrl, urlData.Url/*当前Url*/, _currentDeep + 1/*属于下一层*/, AvailableUrlStatus.UnStart));//标记未开始
                            }
                        }
                        else
                        {
                            //continue;//无效Url，或超出范围
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
                //LogUtility.WebLogger.DebugFormat("url:{0} 线程退出。", url);

                //if (threadStarted)
                {
                    threadInUsing--;
                }
                _remainUrlCount--;

                SenMapicSynData synData = SenMapicSynData.Instance;
                synData.CurrentCrawlingUrlList.Remove(synDataID);
                //allDoneSL.Set();
                //Thread.Sleep(80);//休息一下:)
            }
        }
    }
}