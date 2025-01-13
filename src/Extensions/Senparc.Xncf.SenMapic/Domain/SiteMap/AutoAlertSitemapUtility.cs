using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Data;
// using Ionic.Zip;
// using Senparc.Utility.Senparc.Xncf.SenMapic.Domain.Utility;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    // using Senparc.CO2NET.Trace;
    // using Senparc.Xncf.SenMapic.Domain.Config;
    // using Senparc.Xncf.SenMapic.Domain.Email;
    // using Senparc.Xncf.SenMapic.Domain.Enums;
    // using Senparc.Xncf.SenMapic.Domain.Extensions;
    // using Senparc.Xncf.SenMapic.Domain.Log;
    // using Senparc.Xncf.SenMapic.Domain.Models;
    // using Senparc.Xncf.SenMapic.Domain.Utility;

    public class AutoAlertSitemapUtility
    {
        // /// <summary>
        // /// 只生成一次
        // /// </summary>
        // public bool BuildOnlyOnce { get; set; }
        // public List<int> SiteMapOrderIds { get; set; }
        // public bool SendEmail { get; set; }

        // /// <summary>
        // /// 不要自动发送Email的用户名
        // /// </summary>
        // private string[] notAutoSendUsername = new[] { "ZSU", "ADMIN", "GALIJIKUAI", "ANONYMITY" };
        // private string[] vipUsername = new[] { "ZSU", "ADMIN", "GALIJIKUAI", "ANONYMITY" };
        // private string saveFileDirectory;
        // private readonly int maxBuildThreadCount = 2;//Sitemap Build中最大同时工作线程数（收集页面线程，非当前类同时运行站点线程）
        // private readonly int maxVipBuidThreadCount = 6;//Sitemap Build中最大同时工作线程数（收集页面线程，非当前类同时运行站点线程）
        // private int maxAutoAlertThread = 2;//AutoAlert最大同时收集站点（线程）数
        // private int _autoAlertThreadInUsing = 0;
        // /// <summary>
        // /// 正在使用中的线程数
        // /// </summary>
        // private int autoAlertThreadInUsing
        // {
        //     get
        //     {
        //         lock (syncLock)
        //         {
        //             return _autoAlertThreadInUsing;
        //         }
        //     }
        //     set
        //     {
        //         lock (syncLock)
        //         {
        //             _autoAlertThreadInUsing = value;
        //         }
        //     }
        // }

        // private Semaphore _semaphorePool;
        // private int _semaphorePoolPreviousCount;
        // private int semaphorePoolPreviousCount
        // {
        //     get
        //     {
        //         lock (syncLock)
        //         {
        //             return _semaphorePoolPreviousCount;
        //         }
        //     }
        //     set
        //     {
        //         lock (syncLock)
        //         {
        //             _semaphorePoolPreviousCount = value;
        //         }
        //     }
        // }

        // private int remainQueueOrderCount;

        // private object syncLock = new object();//锁

        // private SenparcEntities ctx;
        // private SendEmail sendEmail;

        // public AutoAlertSitemapUtility() : this(buildOneTime: false, siteMapOrderIds: null, sendEmail: true) { }

        // public AutoAlertSitemapUtility(bool buildOneTime, List<int> siteMapOrderIds, bool sendEmail)
        // {
        //     BuildOnlyOnce = buildOneTime;
        //     SiteMapOrderIds = siteMapOrderIds;
        //     SendEmail = sendEmail;

        //     saveFileDirectory = string.Format(GlobalConfigString.SITEMAP_SAVE_FILE_PATH + "/Sitemap.bak/{0}", DateTime.Now.ToString("yyyy-MM"));
        // }

        // public void BuildLoop()
        // {
        //     if (!BuildOnlyOnce)
        //     {
        //         Thread.Sleep(TimeSpan.FromSeconds(10));//延时
        //     }

        //     do
        //     {
        //         _semaphorePool = new Semaphore(maxAutoAlertThread, maxAutoAlertThread);
        //         SenparcTrace.SendCustomLog("SiteMap", "一次Sitemap循环开始");
        //         CleanStatisticsFiles();//清理历史统计文件
        //         AutoCheckOrderApply();//自动审核定制服务申请
        //         this.BuildOnlyOnceEventHandler(null, false);
        //         SenparcTrace.SendCustomLog("SiteMap", "一次Sitemap循环结束");
        //         _semaphorePool.Close();

        //         if (!BuildOnlyOnce)
        //         {
        //             Thread.Sleep(TimeSpan.FromMinutes(10));//执行间隔10分钟
        //         }
        //     } while (!BuildOnlyOnce);
        // }

        private void BuildOnlyOnceEventHandler(object state, bool timeout)
        {
            // try
            // {
            //     ctx = new SenparcEntities(Senparc.Xncf.SenMapic.Domain.Config.SenparcDatabaseConfigs.ClientConnectionString);
            //     List<SiteMapOrder> siteMapOrders = null;

            //     if (SiteMapOrderIds == null || SiteMapOrderIds.Count == 0)
            //     {
            //         siteMapOrders = ctx.SiteMapOrderSet.Include("UserInfo")
            //             .Where(z => z.UserInfo.UserId == 2)//暂时只处理zsu用户的信息
            //             .OrderBy(z => z.Id)
            //             .ToList();
            //     }
            //     else
            //     {
            //         siteMapOrders = ctx.SiteMapOrderSet.Include("UserInfo").Where(
            //             EntityFrameworkExtensions.BuildContainsExpression<SiteMapOrder, int>(z => z.Id, SiteMapOrderIds)).OrderBy(z => z.Id).ToList();
            //     }

            //     sendEmail = new SendEmail(SendEmailType.SiteMap_Build);

            //     WaitCallback waitCallback = new WaitCallback(BuidSitemapEventHandler);
            //     var allQueueOrders = siteMapOrders.Where(order =>
            //         BuildOnlyOnce
            //         ? true//全部
            //         : (order.LastCreateTime.AddMinutes(order.BuildFrequencyMinutes) < DateTime.Now && order.InUse)//自动循环，只筛选符合条件的记录，避免线程卡死
            //         ).ToList();//所有符合条件的列表

            //     int listedAllOrdersCount = allQueueOrders.Count;
            //     var queueOrders = new List<SiteMapOrder>();//实际轮询操作的列表

            //     //在网站访问量较大的时间段内，每次轮询只处理少数个
            //     if (DateTime.Now.Hour >= 7 && DateTime.Now.Hour <= 21)
            //     {
            //         int takeEndCount = Math.Max(allQueueOrders.Count / 5, 2);//取ID最大的末尾数量（符合条件订单的1/5或2中的较大值）
            //         queueOrders = allQueueOrders.OrderByDescending(z => z.Id).Take(takeEndCount).ToList();//取ID最大的最后N个
            //         if (listedAllOrdersCount > takeEndCount)
            //         {
            //             int takeBeginingCount = 1;//取ID最小的最前面N个
            //             int finalTakeBeginingCount = Math.Min(listedAllOrdersCount - takeEndCount, takeBeginingCount);//最终获取ID最小的个数
            //             queueOrders.AddRange(allQueueOrders.OrderBy(z => z.Id).Take(finalTakeBeginingCount).ToList());//取ID最多ID最小的前N个
            //         }
            //     }
            //     else
            //     {
            //         queueOrders = allQueueOrders.ToList();//不再限制时间段内，全部处理
            //     }

            //     SenparcTrace.SendCustomLog("SiteMap", $"Sitemap预定总数:{siteMapOrders.Count}，符合条件列队数:{listedAllOrdersCount}，实际处理数：{queueOrders.Count}");

            //     DateTime dtStartWait = DateTime.Now;
            //     foreach (var order in queueOrders)
            //     {
            //         bool successQueuesd = ThreadPool.QueueUserWorkItem(waitCallback, order);
            //         if (successQueuesd)
            //         {
            //             remainQueueOrderCount++;//还剩下没处理的Order
            //         }
            //     }

            //     //semaphorePoolPreviousCount = this._semaphorePool.Release(maxAutoAlertThread);//释放所有线程

            //     while (remainQueueOrderCount > 0)
            //     {
            //         if (dtStartWait.AddMinutes(5) <= DateTime.Now)
            //         {
            //             dtStartWait = DateTime.Now;
            //             SenparcTrace.SendCustomLog("SiteMap", "等待线程结束已有5分钟。", 
            //                 new Exception($"总列队数:{queueOrders.Count},使用中的线程数量:{autoAlertThreadInUsing}"));
            //         }

            //         Thread.Sleep(300);
            //     }

            //     //收录时间较长，则记录
            //     if (queueOrders.Count > 0 && (DateTime.Now - dtStartWait).TotalMinutes >= queueOrders.Count * 5)
            //     {
            //         SenparcTrace.SendCustomLog("SiteMap", $"所有站点线程结束,耗时{(DateTime.Now - dtStartWait).TotalMinutes.ToString("#.#")}分钟。",
            //             new Exception($"总列队数:{queueOrders.Count},使用中的线程数量:{autoAlertThreadInUsing}"));
            //     }

            //     if (autoAlertThreadInUsing > 0)
            //     {
            //         SenparcTrace.SendCustomLog("SiteMap", $"AutoAlert线程超出最大值，当前线程数：{autoAlertThreadInUsing}。");
            //     }
            // }
            // catch (Exception e)
            // {
            //     SenparcTrace.SendCustomLog("SiteMap", $"AutoAlertSitemap严重异常:{e.Message}。下一个AutoAlert循环将继续进行。", e);
            //     if (BuildOnlyOnce)
            //     {
            //         throw;
            //     }
            // }
            // finally
            // {
            //     ctx.Dispose();
            //     ctx = null;
            // }
        }

        private void BuidSitemapEventHandler(object state)
        {
            // SiteMapCollection siteMapCollection = null;
            // SiteMapOrder order = state as SiteMapOrder;
            // bool threadStarted = false;
            // try
            // {
            //     _semaphorePool.WaitOne();//列队等待
            //     threadStarted = true;
            //     autoAlertThreadInUsing++;

            //     LogUtility.WebLogger.Debug("自动收集Sitemap:{0} (ID:{1})开始".With(order.Url, order.Id.ToString()));

            //     int oldOrderMaxPageCount = order.MaxPageCount;

            //     //收集Url
            //     string sitemapXmlFileName;
            //     string zipFileName;
            //     string reportFileName;
            //     string sitemapHtmlFileName;
            //     string[] domains = order.Url.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            //     List<string> filterOmitKeywords = order.FilterOmitKeyWords != null ? order.FilterOmitKeyWords.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>();

            //     //当前网站允许最大线程数
            //     var currentMaxThreadCount = vipUsername.Contains(order.UserName.ToUpper()) ? maxVipBuidThreadCount : maxBuildThreadCount;

            //     SenMapicEngine senMapic = new SenMapicEngine(domains, maxVipBuidThreadCount, 30,
            //         order.Priority, order.Changefreq, DateTime.Now, order.Deep, order.MaxPageCount, filterOmitKeywords);

            //     var totalUrls = senMapic.Build();

            //     BuildGoogleSitemapWithReport sitemapWithReport = new BuildGoogleSitemapWithReport(/*maxVipBuidThreadCount, 30*/);
            //     sitemapWithReport.BuildSitemapReport(order.Url, totalUrls, order.Deep, order.MaxPageCount, order.Priority, order.Changefreq,
            //         DateTime.Now, senMapic.FilterOmitWords, out sitemapXmlFileName, out zipFileName, out reportFileName, out sitemapHtmlFileName);


            //     #region 生成新的SiteMapCollection，加入数据库
            //     siteMapCollection = new SiteMapCollection()
            //     {
            //         Guid = Guid.NewGuid(),
            //         SiteMapOrder = order,
            //         CreateTime = DateTime.Now,
            //         SiteMapXML = "",
            //         SiteMapReport = "",
            //         SiteMapHTML = "",
            //         UserName = order.UserName,
            //         Changefreq = order.Changefreq,
            //         Deep = order.Deep,
            //         MaxPageCount = order.MaxPageCount,
            //         Priority = order.Priority,
            //         Url = order.Url,
            //         SuccessPages = senMapic.TotalUrls.Count(z => z.Value.Result == (int)HttpStatusCode.OK),
            //         FailPages = senMapic.TotalUrls.Count(z => z.Value.Result != (int)HttpStatusCode.OK),
            //         AverageRequestMillisecond = Convert.ToInt32(senMapic.AverageRequestTime.TotalMilliseconds)
            //     };

            //     order.LastCreateTime = DateTime.Now;//记录开始的时间。
            //     if (!order.Downloaded.IsNullOrEmpty() && !order.Downloaded.Contains("unlimited"))
            //     {
            //         order.Downloaded = null;//清空下载状态信息  
            //     }
            //     ctx.SiteMapCollectionSet.Add(siteMapCollection);
            //     #endregion

            //     string awardRecord = null;
            //     string punishRecord = null;
            //     var isVip = vipUsername.Contains(order.UserName.ToUpper());
            //     if (order.MaxPageCount < 2000 && !isVip)
            //     {
            //         //只有非VIP且收录上限少于2000的用户，才会增加。

            //         //如果所有Url份额全部完成，则增加一定数量的页面
            //         if ((siteMapCollection.SuccessPages + siteMapCollection.FailPages >= oldOrderMaxPageCount)
            //             && oldOrderMaxPageCount > 0 && ((double)siteMapCollection.FailPages / oldOrderMaxPageCount) <= 0.05)
            //         {
            //             order.MaxPageCount += 2;//TODO:添加响应时间参数
            //             awardRecord += "收录页面达到最大值，且错误页面小于5%，奖励2个页面数量。<br />";
            //         }

            //         //奖励或惩罚平均页面响应时间
            //         AwardOrPunishAverageRequestTime(siteMapCollection, order, ref awardRecord, ref punishRecord);
            //     }
            //     else if (isVip)
            //     {
            //         //扩充vip页面数量，保证有40%富余
            //         if (order.MaxPageCount < siteMapCollection.SuccessPages * 1.4)
            //         {
            //             order.MaxPageCount = (int)(siteMapCollection.SuccessPages * 1.4);
            //         }

            //         LogUtility.SitemapLogger.InfoFormat("VIP Sitemap：{0}，最高页面设定为：{1} -> {2}", order.Url, oldOrderMaxPageCount, order.MaxPageCount);
            //     }

            //     ctx.SaveChanges();

            //     //LogUtility.WebLogger.Debug("自动收集Sitemap:{0}存入数据库完成".With(order.Url));

            //     siteMapCollection = ctx.SiteMapCollectionSet.FirstOrDefault(z => z.Guid == siteMapCollection.Guid);

            //     #region 保存文件
            //     //保存文件
            //     string fileDirectory = Server.HttpContext.Server.MapPath(Config.GlobalConfigString.GetSitemapCollectionFileDirectory(order.Id));
            //     if (!Directory.Exists(fileDirectory))
            //     {
            //         Directory.CreateDirectory(fileDirectory);
            //         IoUtility.AddDirectorySecurity(fileDirectory, "NETWORK SERVICE", System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow);
            //     }

            //     string zipFilePath = Config.GlobalConfigString.GetSitemapCollectionFileName("", order.Id, siteMapCollection.Id, "zip");
            //     using (ZipFile zipFile = new ZipFile(Server.GetMapPath(zipFilePath)))
            //     {
            //         //Sitemap Report
            //         string sitemapReport = new Regex(@"(?<=<!-- report start -->)([\s\W\w]*)(?=<!-- report end -->)", RegexOptions.IgnoreCase)
            //                     .Match(File.OpenText(reportFileName).ReadToEnd()).Value.Trim();//通过正则查找内容部分
            //         //string reportFilePath = Config.GlobalConfigString.GetSitemapCollectionFileName("-report", order.Id, siteMapCollection.Id, "html");
            //         zipFile.AddEntry("report.html", null, sitemapReport);

            //         //Sitemap XML
            //         //string sitemapXmlFilePath = Config.GlobalConfigString.GetSitemapCollectionFileName("", order.Id, siteMapCollection.Id, "xml");
            //         var srXML = File.OpenText(sitemapXmlFileName);
            //         zipFile.AddEntry("sitemap.xml", null, srXML.BaseStream);

            //         //Sitemap HTML
            //         //string sitemapHtmlFilePath = Config.GlobalConfigString.GetSitemapCollectionFileName("", order.Id, siteMapCollection.Id, "html");
            //         var srHTML = File.OpenText(sitemapHtmlFileName);
            //         zipFile.AddEntry("sitemap.html", null, srHTML.BaseStream);

            //         //zipFile.AddFiles(new[] { sitemapXmlFileName, sitemapHtmlFileName },false,null);
            //         zipFile.Save();
            //         srXML.Close();
            //         srHTML.Close();
            //     }
            //     #endregion

            //     siteMapCollection.SiteMapXML = zipFilePath;//sitemapXmlFilePath;
            //     siteMapCollection.SiteMapReport = zipFilePath;//reportFilePath;
            //     siteMapCollection.SiteMapHTML = zipFilePath;// sitemapHtmlFilePath;
            //     ctx.SaveChanges();

            //     #region callback

            //     if (!order.Callback.IsNullOrEmpty())
            //     {
            //         try
            //         {
            //             //SiteMapCallback("http://www.senparc.com/Home.htm", "/SZD-251");
            //             SiteMapCallback(order.Url, order.Callback);

            //             LogUtility.SitemapLogger.InfoFormat("Sitemap Callback成功：#{0}，Url：{1},Callback：{2}", order.Id, order.Url, order.Callback);
            //         }
            //         catch (Exception e)
            //         {
            //             LogUtility.SitemapLogger.Error("Sitemap Callback错误：" + e.Message, e);
            //         }
            //     }

            //     #endregion

            //     #region 发送Email
            //     if (SendEmail)
            //     {
            //         //不在过滤范围内的用户，且不是只发送一次，则自动发送Email
            //         if (awardRecord.IsNullOrEmpty())
            //         {
            //             awardRecord = "无";
            //         }
            //         if (punishRecord.IsNullOrEmpty())
            //         {
            //             punishRecord = "无";
            //         }

            //         bool sendImmediately = !notAutoSendUsername.Contains(order.UserName.ToUpper()) && !BuildOnlyOnce;
            //         SendEmailParameter_SiteMap_Build sendEmailParam =
            //             new SendEmailParameter_SiteMap_Build(order.UserInfo.Email, order.UserName,
            //                 siteMapCollection.Id, order.Id, order.Url,
            //                 siteMapCollection.SuccessPages, siteMapCollection.FailPages, siteMapCollection.MaxPageCount,
            //                 siteMapCollection.AverageRequestMillisecond, senMapic.TotalPageSizeKB.ToString("###,##0.000"),
            //                 oldOrderMaxPageCount, order.MaxPageCount, order.Deep,
            //                 awardRecord, punishRecord);
            //         if (sendImmediately)
            //         {
            //             //只发送非系统用户的
            //             sendEmail.Send(sendEmailParam, true, sendImmediately, true);
            //         }
            //         else
            //         {
            //             LogUtility.EmailLogger.InfoFormat("VIP用户收集Sitemap完成，未发送Email：{0},{1}", order.UserName, order.Url);
            //         }
            //     }
            //     #endregion

            //     CheckLoginAndAlert(order);//判断是否超时未登录
            // }
            // catch (Exception e)
            // {
            //     try
            //     {
            //         //TODO:跟踪莫名其妙自动收录停止
            //         LogUtility.SitemapLogger.Debug("跟踪莫名其妙自动收录停止，到达catch（AutoAlertSitemapUtility.cs第350行）", e);
            //         LogUtility.SitemapLogger.Error("AutoAlertSitemap自动发送Email出错:{0}.URL:{1}".With(e.Message, siteMapCollection != null ? siteMapCollection.Url : order.Url), e);
            //     }
            //     catch (Exception ex)
            //     {
            //         LogUtility.SitemapLogger.Error("AutoAlertSitemapUtility.cs第356行错误！！catch中再次发生错误", ex);
            //     }

            //     //AutoSendLogEmail.SendLogEmail(e);
            // }
            // finally
            // {
            //     //LogUtility.WebLogger.Debug("自动收集Sitemap:{0}发送Email完成".With(order.Url));
            //     semaphorePoolPreviousCount = _semaphorePool.Release();
            //     remainQueueOrderCount--;
            //     if (threadStarted)
            //     {
            //         autoAlertThreadInUsing--;
            //     }
            // }
        }

        /// <summary>
        /// 检查是否超时未登录，如果是，则发送Email提示
        /// </summary>
        /// <param name="order"></param>
        private void CheckLoginAndAlert(SiteMapOrder order)
        {
            // DateTime currentLoginTime = order.UserInfo.CurrentLoginTime;
            // TimeSpan tsAllowMaxUnloginTime = TimeSpan.FromDays(SystemParameters.SitemapLogExpireDays);
            // if (currentLoginTime.Add(tsAllowMaxUnloginTime) < order.LastCreateTime)
            // {
            //     if (notAutoSendUsername.Contains(order.UserName.ToUpper()))
            //     {
            //         return;//指定用户不提示
            //     }


            //     //已超时
            //     if (currentLoginTime.Add(tsAllowMaxUnloginTime + TimeSpan.FromDays(15)) < order.LastCreateTime)
            //     {
            //         //超时15天，自动删除
            //         SendEmail sendAlertEmail = new SendEmail(SendEmailType.SiteMap_Remove);
            //         var sendEmailParam = new SendEmailParameter_SiteMap_Remove(order.UserInfo.Email, order.UserName, order.Id, order.Url);
            //         sendAlertEmail.Send(sendEmailParam, true, true, true);

            //         //删除文件
            //         SiteMapHandler siteMapHandler = new SiteMapHandler();
            //         siteMapHandler.RemoveSiteMapCollectionFiles(order);

            //         //ctx.DeleteObject(order);//从数据库删除order，下属orderCollection会被连带删除
            //         ctx.SiteMapOrderSet.Remove(order);

            //         ctx.SaveChanges();
            //         LogUtility.SitemapLogger.InfoFormat("Sitemap超过登陆天数被自动删除：{0}，{1}", order.UserName, order.Url);
            //     }
            //     else if (currentLoginTime.Add(tsAllowMaxUnloginTime + TimeSpan.FromDays(5)) < order.LastCreateTime)
            //     {
            //         //超时5天，自动关闭
            //         SendEmail sendAlertEmail = new SendEmail(SendEmailType.SiteMap_LoginExpired);
            //         var sendEmailParam = new SendEmailParameter_SiteMap_LoginExpired(order.UserInfo.Email, order.UserName, order.UserInfo.CurrentLoginTime, order.Id, order.Url);
            //         sendAlertEmail.Send(sendEmailParam, true, true, true);

            //         order.InUse = false;
            //         ctx.SaveChanges();
            //     }
            // }
        }

        // /// <summary>
        // /// 奖励或惩罚平均页面响应时间
        // /// </summary>
        // /// <param name="siteMapCollection">当前最新的收录结果</param>
        // /// <param name="order">Order</param>
        // private void AwardOrPunishAverageRequestTime(SiteMapCollection siteMapCollection, SiteMapOrder order, ref string awardRecord, ref string punishRecord)
        // {
        //     //页面奖励或惩罚
        //     int minMillsecond = 350;//平均响应时间最小值，小于此数值奖励
        //     int maxMillisecond = 1200;//平均响应时间最大值，大于此数值惩罚
        //     int currentAverageRequestMS = siteMapCollection.AverageRequestMillisecond;
        //     if (((currentAverageRequestMS > maxMillisecond || currentAverageRequestMS == 0) && order.MaxPageCount > 1)
        //         || currentAverageRequestMS < minMillsecond)
        //     {
        //         int lookBackRecord = 2;//向前看2条记录
        //         var lastCollection = ctx.SiteMapCollectionSet.Include("SiteMapOrder").Where(z => z.SiteMapOrder.Id == order.Id).OrderByDescending(z => z.Id).Take(lookBackRecord).ToList();

        //         //查看前两次都大于最大值
        //         if (lastCollection.Count == lookBackRecord)
        //         {
        //             List<int> averageRequestMillisecondList = new List<int>();//响应时间集合
        //             averageRequestMillisecondList.AddRange(lastCollection.Select(z => z.AverageRequestMillisecond).ToList());
        //             averageRequestMillisecondList.Add(currentAverageRequestMS);

        //             for (int i = 0; i < averageRequestMillisecondList.Count; i++)
        //             {
        //                 if (averageRequestMillisecondList[i] <= 0)
        //                 {
        //                     averageRequestMillisecondList[i] = 99999;//如果无响应，视为响应时间过长
        //                 }
        //             }

        //             #region 此方法无效。由于Int是值类型，这里赋值不会影响到averageRequestMillisecondList中的值
        //             //averageRequestMillisecondList.ForEach(z =>
        //             //{
        //             //    if (z <= 0)
        //             //    {
        //             //        z = 999999;//没有响应视为响应时间无限长
        //             //    }
        //             //});
        //             #endregion

        //             //检查是大雨最大只或小于最小值
        //             Func<int, bool> checkAboveMaxMillisecond = (max) => averageRequestMillisecondList.Count(ms => ms > max) == averageRequestMillisecondList.Count;
        //             Func<int, bool> checkBelowMinMillisecond = (min) => averageRequestMillisecondList.Count(ms => ms < min) == averageRequestMillisecondList.Count;

        //             if (checkAboveMaxMillisecond(maxMillisecond))
        //             {
        //                 int cutPageCount = 10;//扣除页面数
        //                 int maxMS = 1200;//最大毫秒
        //                 //分3个等级判断
        //                 if (checkAboveMaxMillisecond(3000))
        //                 {
        //                     maxMS = 3000;
        //                     cutPageCount = 50;
        //                 }
        //                 else if (checkAboveMaxMillisecond(2000))
        //                 {
        //                     maxMS = 2000;
        //                     cutPageCount = 20;
        //                 }

        //                 punishRecord += "连续{0}次响应时间大于{1}（毫秒）,页面收录数扣减{2}个。<br />".With(averageRequestMillisecondList.Count, maxMS, cutPageCount);

        //                 order.MaxPageCount = Math.Max(1, order.MaxPageCount - cutPageCount);//减N个页面
        //                 LogUtility.SitemapLogger.InfoFormat("订单 #{0}({1})连续{2}次响应时间大于{3}（毫秒），最大收录数被扣减{4}页面"
        //                                                 , order.Id, order.Url, averageRequestMillisecondList.Count, maxMillisecond, cutPageCount);
        //             }
        //             else if (checkBelowMinMillisecond(minMillsecond))
        //             {
        //                 order.MaxPageCount += 2;
        //                 awardRecord += "连续{0}次响应时间小于{1}（毫秒）,奖励{2}个页面数量。<br />".With(lookBackRecord + 1, minMillsecond, 2);

        //                 LogUtility.SitemapLogger.InfoFormat("订单 #{0}({1})连续{2}次响应时间小于{3}（毫秒），奖励2个最大页面收录数量"
        //                                                 , order.Id, order.Url, averageRequestMillisecondList.Count, minMillsecond);
        //             }
        //         }
        //     }
        // }

        // /// <summary>
        // /// 清理自动生成的sitemap及报告文件，并统计到info.text中
        // /// </summary>
        // private void CleanStatisticsFiles()
        // {
        //     if (DateTime.Now.Day != 1)
        //     {
        //         return;//只在1号执行
        //     }

        //     LogUtility.SitemapLogger.Info("清理并统计日志开始");

        //     var historyDirectories = Directory.GetDirectories(Server.HttpContext.Server.MapPath("~/App_Data/SiteMap/Sitemap.bak/"))
        //                                     .Where(z => Path.GetFileName(z) != DateTime.Now.ToString("yyyy-MM"))//当月信息不清理
        //                                     .OrderBy(z => z);

        //     foreach (var dir in historyDirectories)
        //     {
        //         var files = Directory.GetFiles(dir);
        //         int filesCount = files.Length;
        //         bool hasInfoFile = files.Count(z => Path.GetFileName(z) == "info.txt") > 0;
        //         if (!hasInfoFile)
        //         {
        //             LogUtility.SitemapLogger.InfoFormat("创建{0}/info.txt", Path.GetFileNameWithoutExtension(dir));

        //             string infoFileName = Path.Combine(Path.GetFullPath(dir), "info.txt");
        //             TextWriter tw = File.CreateText(infoFileName);
        //             int totalFileCount = files.Length;
        //             int sitemapXmlCount = files.Count(z => Path.GetExtension(z) == ".xml");
        //             int sitemapHtmlCount = files.Count(z => z.Contains("sitemap.html"));
        //             int reportCount = files.Count(z => z.Contains("report"));

        //             StringBuilder text = new StringBuilder();
        //             text.AppendLine("TotalFileCount=" + totalFileCount);
        //             text.AppendLine("SitemapXmlCount=" + sitemapXmlCount);
        //             text.AppendLine("SitemapHtmlCount=" + sitemapHtmlCount);
        //             text.AppendLine("ReportCount=" + reportCount);

        //             //计算每天sitemap.xml文件数量
        //             DateTime dt = DateTime.Parse(Path.GetFileNameWithoutExtension(dir) + "-1");//获取当前月1号
        //             int daysInMonth = DateTime.DaysInMonth(dt.Year, dt.Month);//本月天数
        //             Dictionary<int, List<string>> dicFilesInDay = new Dictionary<int, List<string>>();//分类
        //             for (int i = 1; i <= daysInMonth; i++)
        //             {
        //                 dicFilesInDay.Add(i, new List<string>());
        //             }
        //             foreach (var file in files)
        //             {
        //                 DateTime fileCreationTime = File.GetCreationTime(file);
        //                 if (dicFilesInDay.ContainsKey(fileCreationTime.Day))
        //                 {
        //                     dicFilesInDay[fileCreationTime.Day].Add(file);
        //                 }
        //                 else
        //                 {
        //                     dicFilesInDay.Last().Value.Add(file);//如果不存在，加到最后一天（通常发生在月末，跨两个月的收录）
        //                 }
        //             }
        //             //输出统计结果
        //             string dayXmlCount = string.Join(",", dicFilesInDay.Select(z => z.Value.Count.ToString()).ToArray());
        //             text.AppendLine("DayXmlCount=" + dayXmlCount);

        //             LogUtility.SitemapLogger.InfoFormat("清理统计文件结束，信息:\r\n{0}", text.ToString());
        //             tw.Write(text);
        //             tw.Flush();
        //             tw.Close();

        //             if (totalFileCount != sitemapXmlCount + sitemapHtmlCount + reportCount)
        //             {
        //                 LogUtility.SitemapLogger.ErrorFormat("清理SItemap历史记录出错，数字不匹配，请检查。文件夹：{0}", Path.GetFileName(dir));
        //             }
        //             else
        //             {
        //                 //核算正确，删除多余文件
        //                 try
        //                 {
        //                     LogUtility.SitemapLogger.InfoFormat("删除自动生成的文件 开始。");
        //                     foreach (var delFile in files)
        //                     {
        //                         File.Delete(delFile);
        //                     }
        //                     LogUtility.SitemapLogger.InfoFormat("删除自动生成的文件 结束。");
        //                 }
        //                 catch
        //                 {
        //                     LogUtility.SitemapLogger.ErrorFormat("删除{0}目录文件失败。", Path.GetFileName(dir));
        //                 }
        //                 LogUtility.SitemapLogger.InfoFormat("清理{0}目录完成。", Path.GetFileName(dir));
        //             }
        //         }
        //     }
        //     LogUtility.SitemapLogger.Info("清理并统计日志结束");
        // }

        // /// <summary>
        // /// 自动审核Sitemap定制申请
        // /// </summary>
        // private void AutoCheckOrderApply()
        // {
        //     SenparcEntities ctx = new SenparcEntities(Senparc.Xncf.SenMapic.Domain.Config.SenparcDatabaseConfigs.ClientConnectionString);
        //     DateTime dtLimit = DateTime.Now.AddDays(-3);//搜索3天以内的
        //     List<SiteMapOrder> orders = ctx.SiteMapOrderSet
        //                                     .Include("UserInfo")
        //                                     .Include("SiteMapCollections")
        //                                     .Where(z => z.InUse == false
        //                                         && z.LastCreateTime >= dtLimit
        //                                         && z.SiteMapCollections.Count == 0)
        //                                     .OrderByDescending(z => z.Id)
        //                                     .Take(2).ToList();
        //     foreach (var order in orders)
        //     {
        //         //查看是否能访问
        //         //UrlData urlData = null;

        //         try
        //         {
        //             SenMapicEngine senMapic = new SenMapicEngine(new string[] { order.Url }, 1, 1, null, null, null, 1, 1, null);
        //             //urlData = senMapic.CrawSingleUrl(order.Url);
        //             var result = senMapic.Build();

        //             if (result.Count > 0 && result.First().Value.Result == 200)
        //             {
        //                 SendEmail sendApplyPassedEmail = new Email.SendEmail(SendEmailType.SiteMap_ApplyPassed);
        //                 //发送申请通过通知
        //                 SendEmailParameter_SiteMap_ApplyPassed sendEmailParam = new SendEmailParameter_SiteMap_ApplyPassed(order.UserInfo.Email, order.UserInfo.UserName, order.Id, order.Url);
        //                 sendApplyPassedEmail.Send(sendEmailParam,
        //                                             lineInCache: true,
        //                                             sendImmediately: true,
        //                                             useBackupEmail: true);
        //                 //修改Order信息
        //                 order.InUse = true;//启用定制
        //                 order.LastCreateTime = order.LastCreateTime.AddDays(-1);//设为上一天，在下次轮询时自动收集
        //                 ctx.SaveChanges();

        //                 LogUtility.SitemapLogger.Info("Sitemap定制服务自动审核通过：(#{0}) {1}".With(order.Id, order.Url));
        //             }
        //             else
        //             {
        //                 LogUtility.SitemapLogger.Error("Sitemap定制服务审核失败：(#{0}) {1}".With(order.Id, order.Url));
        //                 //TODO:发邮件通知
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //             LogUtility.SitemapLogger.Error("自动审核定制服务异常：(#{0}) {1}"
        //                 .With(order.Id, e.Message), e);
        //         }
        //     }
        // }

        // /// <summary>
        // /// Sitemap自动更新Callback
        // /// </summary>
        // /// <param name="url"></param>
        // /// <param name="callbackUrl"></param>
        // private void SiteMapCallback(string url, string callbackUrl)
        // {
        //     if (!callbackUrl.StartsWith("/"))
        //     {
        //         callbackUrl = "/" + callbackUrl;
        //     }

        //     Regex regex = new Regex(@"(^http(s)?://[^/]+)");
        //     var match = regex.Match(url);
        //     if (!match.Success)
        //     {
        //         throw new Exception("URL地址无法匹配！");
        //     }
        //     string domain = match.Value;

        //     string fullCallbackUrl = domain + callbackUrl;

        //     HttpWebRequest webRequest = HttpWebRequest.Create(fullCallbackUrl) as HttpWebRequest;
        //     webRequest.Method = "GET";

        //     webRequest.MaximumAutomaticRedirections = 4;

        //     webRequest.Headers.Add(HttpRequestHeader.ContentLanguage, "zh-CN");
        //     //webRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
        //     webRequest.KeepAlive = true;
        //     webRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.2)";

        //     webRequest.Accept = "image/jpeg, application/x-ms-application, image/gif, application/xaml+xml, image/pjpeg, application/x-ms-xbap, application/x-shockwave-flash, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
        //     webRequest.Method = "GET";

        //     HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
        //     using (StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
        //     {
        //         for (int i = 0; i < 10; i++)
        //         {
        //             sr.ReadLine();
        //         }
        //     }

        //     var statusCode = webResponse.StatusCode;


        // }
    }
}
