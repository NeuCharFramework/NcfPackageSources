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
        // /// Only generated once
        // /// </summary>
        // public bool BuildOnlyOnce { get; set; }
        // public List<int> SiteMapOrderIds { get; set; }
        // public bool SendEmail { get; set; }

        // /// <summary>
        // /// Do not automatically send email username
        // /// </summary>
        // private string[] notAutoSendUsername = new[] { "ZSU", "ADMIN", "GALIJIKUAI", "ANONYMITY" };
        // private string[] vipUsername = new[] { "ZSU", "ADMIN", "GALIJIKUAI", "ANONYMITY" };
        // private string saveFileDirectory;
        // private readonly int maxBuildThreadCount = 2;//The maximum number of simultaneous working threads in Sitemap Build (collection page threads, non-current classes run site threads at the same time)
        // private readonly int maxVipBuidThreadCount = 6;//The maximum number of simultaneous working threads in Sitemap Build (collection page threads, non-current classes run site threads at the same time)
        // private int maxAutoAlertThread = 2;//AutoAlert maximum number of simultaneous collection sites (threads)
        // private int _autoAlertThreadInUsing = 0;
        // /// <summary>
        // /// Number of threads in use
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

        // private object syncLock = new object();//lock

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
        //         Thread.Sleep(TimeSpan.FromSeconds(10));//Delay
        //     }

        //     do
        //     {
        //         _semaphorePool = new Semaphore(maxAutoAlertThread, maxAutoAlertThread);
        //         SenparcTrace.SendCustomLog("SiteMap", "A Sitemap cycle begins");
        //         CleanStatisticsFiles();//Clean historical statistics files
        //         AutoCheckOrderApply();//Automatically review custom service applications
        //         this.BuildOnlyOnceEventHandler(null, false);
        //         SenparcTrace.SendCustomLog("SiteMap", "A Sitemap cycle ends");
        //         _semaphorePool.Close();

        //         if (!BuildOnlyOnce)
        //         {
        //             Thread.Sleep(TimeSpan.FromMinutes(10));//Execution interval is 10 minutes
        //         }
        //     } while (!BuildOnlyOnce);
        // }

        // private void BuildOnlyOnceEventHandler(object state, bool timeout)
        // {
            // try
            // {
            //     ctx = new SenparcEntities(Senparc.Xncf.SenMapic.Domain.Config.SenparcDatabaseConfigs.ClientConnectionString);
            //     List<SiteMapOrder> siteMapOrders = null;

            //     if (SiteMapOrderIds == null || SiteMapOrderIds.Count == 0)
            //     {
            //         siteMapOrders = ctx.SiteMapOrderSet.Include("UserInfo")
            //             .Where(z => z.UserInfo.UserId == 2)//Only process zsu user information for now
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
            //         ? true//all
            //         : (order.LastCreateTime.AddMinutes(order.BuildFrequencyMinutes) < DateTime.Now && order.InUse)//Automatically loop, only filter records that meet the conditions to avoid thread stuck
            //         ).ToList();//All lists that meet the conditions

            //     int listedAllOrdersCount = allQueueOrders.Count;
            //     var queueOrders = new List<SiteMapOrder>();//List of actual polling operations

            //     //During the period of time when the website has a large number of visits, only a few will be processed in each poll.
            //     if (DateTime.Now.Hour >= 7 && DateTime.Now.Hour <= 21)
            //     {
            //         int takeEndCount = Math.Max(allQueueOrders.Count / 5, 2);//Take the largest end number of IDs (the larger of 1/5 or 2 of the eligible orders)
            //         queueOrders = allQueueOrders.OrderByDescending(z => z.Id).Take(takeEndCount).ToList();//Take the last N with the largest ID
            //         if (listedAllOrdersCount > takeEndCount)
            //         {
            //             int takeBeginingCount = 1; //Take the first N numbers with the smallest ID
            //             int finalTakeBeginingCount = Math.Min(listedAllOrdersCount - takeEndCount, takeBeginingCount);//Finally get the smallest number of IDs
            //             queueOrders.AddRange(allQueueOrders.OrderBy(z => z.Id).Take(finalTakeBeginingCount).ToList());//Get the top N with the largest ID and the smallest ID
            //         }
            //     }
            //     else
            //     {
            //         queueOrders = allQueueOrders.ToList();//No longer limited time period, all processes
            //     }

            //     SenparcTrace.SendCustomLog("SiteMap", $"Total number of Sitemap reservations: {siteMapOrders.Count}, number of qualifying queues: {listedAllOrdersCount}, actual number of processes: {queueOrders.Count}");

            //     DateTime dtStartWait = DateTime.Now;
            //     foreach (var order in queueOrders)
            //     {
            //         bool successQueuesd = ThreadPool.QueueUserWorkItem(waitCallback, order);
            //         if (successQueuesd)
            //         {
            //             remainQueueOrderCount++;//There are still unprocessed Orders left
            //         }
            //     }

            //     //semaphorePoolPreviousCount = this._semaphorePool.Release(maxAutoAlertThread);//Release all threads

            //     while (remainQueueOrderCount > 0)
            //     {
            //         if (dtStartWait.AddMinutes(5) <= DateTime.Now)
            //         {
            //             dtStartWait = DateTime.Now;
            //             SenparcTrace.SendCustomLog("SiteMap", "It has been 5 minutes waiting for the thread to end.", 
            //                 new Exception($"Total number of queues: {queueOrders.Count}, number of threads in use: {autoAlertThreadInUsing}"));
            //         }

            //         Thread.Sleep(300);
            //     }

            //     //The collection time is longer, then record
            //     if (queueOrders.Count > 0 && (DateTime.Now - dtStartWait).TotalMinutes >= queueOrders.Count * 5)
            //     {
            //         SenparcTrace.SendCustomLog("SiteMap", $"All site threads ended, taking {(DateTime.Now - dtStartWait).TotalMinutes.ToString("#.#")} minutes.",
            //             new Exception($"Total number of queues: {queueOrders.Count}, number of threads in use: {autoAlertThreadInUsing}"));
            //     }

            //     if (autoAlertThreadInUsing > 0)
            //     {
            //         SenparcTrace.SendCustomLog("SiteMap", $"AutoAlert thread exceeds the maximum value, current number of threads: {autoAlertThreadInUsing}.");
            //     }
            // }
            // catch (Exception e)
            // {
            //     SenparcTrace.SendCustomLog("SiteMap", $"AutoAlertSitemap serious exception: {e.Message}. The next AutoAlert loop will continue.", e);
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
        // }

        // private void BuidSitemapEventHandler(object state)
        // {
            // SiteMapCollection siteMapCollection = null;
            // SiteMapOrder order = state as SiteMapOrder;
            // bool threadStarted = false;
            // try
            // {
            //     _semaphorePool.WaitOne();//Waiting in queue
            //     threadStarted = true;
            //     autoAlertThreadInUsing++;

            //     LogUtility.WebLogger.Debug("Automatic collection of Sitemap:{0} (ID:{1}) starts".With(order.Url, order.Id.ToString()));

            //     int oldOrderMaxPageCount = order.MaxPageCount;

            //     //Collect Url
            //     string sitemapXmlFileName;
            //     string zipFileName;
            //     string reportFileName;
            //     string sitemapHtmlFileName;
            //     string[] domains = order.Url.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            //     List<string> filterOmitKeywords = order.FilterOmitKeyWords != null ? order.FilterOmitKeyWords.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>();

            //     //The maximum number of threads allowed by the current website
            //     var currentMaxThreadCount = vipUsername.Contains(order.UserName.ToUpper()) ? maxVipBuidThreadCount : maxBuildThreadCount;

            //     SenMapicEngine senMapic = new SenMapicEngine(domains, maxVipBuidThreadCount, 30,
            //         order.Priority, order.Changefreq, DateTime.Now, order.Deep, order.MaxPageCount, filterOmitKeywords);

            //     var totalUrls = senMapic.Build();

            //     BuildGoogleSitemapWithReport sitemapWithReport = new BuildGoogleSitemapWithReport(/*maxVipBuidThreadCount, 30*/);
            //     sitemapWithReport.BuildSitemapReport(order.Url, totalUrls, order.Deep, order.MaxPageCount, order.Priority, order.Changefreq,
            //         DateTime.Now, senMapic.FilterOmitWords, out sitemapXmlFileName, out zipFileName, out reportFileName, out sitemapHtmlFileName);


            //     #region Generate a new SiteMapCollection and add it to the database
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

            //     order.LastCreateTime = DateTime.Now;//The time when the record starts.
            //     if (!order.Downloaded.IsNullOrEmpty() && !order.Downloaded.Contains("unlimited"))
            //     {
            //         order.Downloaded = null;//Clear download status information  
            //     }
            //     ctx.SiteMapCollectionSet.Add(siteMapCollection);
            //     #endregion

            //     string awardRecord = null;
            //     string punishRecord = null;
            //     var isVip = vipUsername.Contains(order.UserName.ToUpper());
            //     if (order.MaxPageCount < 2000 && !isVip)
            //     {
            //         //Only non-VIP users with an upper limit of less than 2,000 will be added.

            //         //If all Url shares are completed, add a certain number of pages
            //         if ((siteMapCollection.SuccessPages + siteMapCollection.FailPages >= oldOrderMaxPageCount)
            //             && oldOrderMaxPageCount > 0 && ((double)siteMapCollection.FailPages / oldOrderMaxPageCount) <= 0.05)
            //         {
            //             order.MaxPageCount += 2;//TODO: Add response time parameter
            //             awardRecord += "The included pages have reached the maximum value, and the error pages are less than 5%, and the number of pages awarded is 2.<br />";
            //         }

            //         //Reward or punish average page response time
            //         AwardOrPunishAverageRequestTime(siteMapCollection, order, ref awardRecord, ref punishRecord);
            //     }
            //     else if (isVip)
            //     {
            //         //Expand the number of VIP pages to ensure 40% surplus
            //         if (order.MaxPageCount < siteMapCollection.SuccessPages * 1.4)
            //         {
            //             order.MaxPageCount = (int)(siteMapCollection.SuccessPages * 1.4);
            //         }

            //         LogUtility.SitemapLogger.InfoFormat("VIP Sitemap: {0}, the highest page setting is: {1} -> {2}", order.Url, oldOrderMaxPageCount, order.MaxPageCount);
            //     }

            //     ctx.SaveChanges();

            //     //LogUtility.WebLogger.Debug("Automatic collection of Sitemap: {0} is completed and stored in the database".With(order.Url));

            //     siteMapCollection = ctx.SiteMapCollectionSet.FirstOrDefault(z => z.Guid == siteMapCollection.Guid);

            //     #region Save file
            //     //save file
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
            //                     .Match(File.OpenText(reportFileName).ReadToEnd()).Value.Trim();//Find the content part through regular expressions
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

            //             LogUtility.SitemapLogger.InfoFormat("Sitemap Callback successful: #{0}, Url: {1}, Callback: {2}", order.Id, order.Url, order.Callback);
            //         }
            //         catch (Exception e)
            //         {
            //             LogUtility.SitemapLogger.Error("Sitemap Callback error: " + e.Message, e);
            //         }
            //     }

            //     #endregion

            //     #region Send Email
            //     if (SendEmail)
            //     {
            //         //For users who are not within the filtering range, and if the email is not sent only once, the email will be automatically sent.
            //         if (awardRecord.IsNullOrEmpty())
            //         {
            //             awardRecord = "None";
            //         }
            //         if (punishRecord.IsNullOrEmpty())
            //         {
            //             punishRecord = "None";
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
            //             //Only send non-system users
            //             sendEmail.Send(sendEmailParam, true, sendImmediately, true);
            //         }
            //         else
            //         {
            //             LogUtility.EmailLogger.InfoFormat("VIP user Sitemap collection completed, but no email sent: {0}, {1}", order.UserName, order.Url);
            //         }
            //     }
            //     #endregion

            //     CheckLoginAndAlert(order);//Determine whether the timeout has not logged in
            // }
            // catch (Exception e)
            // {
            //     try
            //     {
            //         //TODO: Tracking is inexplicably stopped automatically.
            //         LogUtility.SitemapLogger.Debug("Tracking inexplicably stopped automatically, reaching catch (AutoAlertSitemapUtility.cs line 350)", e);
            //         LogUtility.SitemapLogger.Error("AutoAlertSitemap automatically sent Email error: {0}.URL:{1}".With(e.Message, siteMapCollection != null ? siteMapCollection.Url : order.Url), e);
            //     }
            //     catch (Exception ex)
            //     {
            //         LogUtility.SitemapLogger.Error("Error on line 356 of AutoAlertSitemapUtility.cs!! An error occurred again in catch", ex);
            //     }

            //     //AutoSendLogEmail.SendLogEmail(e);
            // }
            // finally
            // {
            //     //LogUtility.WebLogger.Debug("Automatic collection of Sitemap: {0} completion of sending email".With(order.Url));
            //     semaphorePoolPreviousCount = _semaphorePool.Release();
            //     remainQueueOrderCount--;
            //     if (threadStarted)
            //     {
            //         autoAlertThreadInUsing--;
            //     }
            // }
        // }

        /// <summary>
        /// Check whether the user has not logged in after timeout, if so, send an email prompt
        /// </summary>
        /// <param name="order"></param>
        // private void CheckLoginAndAlert(SiteMapOrder order)
        // {
          // DateTime currentLoginTime = order.UserInfo.CurrentLoginTime;
            // TimeSpan tsAllowMaxUnloginTime = TimeSpan.FromDays(SystemParameters.SitemapLogExpireDays);
            // if (currentLoginTime.Add(tsAllowMaxUnloginTime) < order.LastCreateTime)
            // {
            //     if (notAutoSendUsername.Contains(order.UserName.ToUpper()))
            //     {
            //         return;//Specify the user not to prompt
            //     }

            //     //Timed out
            //     if (currentLoginTime.Add(tsAllowMaxUnloginTime + TimeSpan.FromDays(15)) < order.LastCreateTime)
            //     {
            //         //Timeout 15 days, automatically deleted
            //         SendEmail sendAlertEmail = new SendEmail(SendEmailType.SiteMap_Remove);
            //         var sendEmailParam = new SendEmailParameter_SiteMap_Remove(order.UserInfo.Email, order.UserName, order.Id, order.Url);
            //         sendAlertEmail.Send(sendEmailParam, true, true, true);

            //         //delete file
            //         SiteMapHandler siteMapHandler = new SiteMapHandler();
            //         siteMapHandler.RemoveSiteMapCollectionFiles(order);

            //         //ctx.DeleteObject(order);//Delete order from the database, and the subordinate orderCollection will be deleted simultaneously
            //         ctx.SiteMapOrderSet.Remove(order);

            //         ctx.SaveChanges();
            //         LogUtility.SitemapLogger.InfoFormat("Sitemap will be automatically deleted after the number of login days: {0}, {1}", order.UserName, order.Url);
            //     }
            //     else if (currentLoginTime.Add(tsAllowMaxUnloginTime + TimeSpan.FromDays(5)) < order.LastCreateTime)
            //     {
            //         //Timeout 5 days, automatically shut down
            //         SendEmail sendAlertEmail = new SendEmail(SendEmailType.SiteMap_LoginExpired);
            //         var sendEmailParam = new SendEmailParameter_SiteMap_LoginExpired(order.UserInfo.Email, order.UserName, order.UserInfo.CurrentLoginTime, order.Id, order.Url);
            //         sendAlertEmail.Send(sendEmailParam, true, true, true);

            //         order.InUse = false;
            //         ctx.SaveChanges();
            //     }
            // }
        }

        // /// <summary>
        // /// Reward or punish average page response time
        // /// </summary>
        // /// <param name="siteMapCollection">The latest collection results</param>
        // /// <param name="order">Order</param>
        // private void AwardOrPunishAverageRequestTime(SiteMapCollection siteMapCollection, SiteMapOrder order, ref string awardRecord, ref string punishRecord)
        // {
        //     //Page rewards or penalties
        //     int minMillsecond = 350; //Minimum average response time, rewards less than this value
        //     int maxMillisecond = 1200; //Maximum average response time, penalty for greater than this value
        //     int currentAverageRequestMS = siteMapCollection.AverageRequestMillisecond;
        //     if (((currentAverageRequestMS > maxMillisecond || currentAverageRequestMS == 0) && order.MaxPageCount > 1)
        //         || currentAverageRequestMS < minMillsecond)
        //     {
        //         int lookBackRecord = 2;//Look forward 2 records
        //         var lastCollection = ctx.SiteMapCollectionSet.Include("SiteMapOrder").Where(z => z.SiteMapOrder.Id == order.Id).OrderByDescending(z => z.Id).Take(lookBackRecord).ToList();

        //         //Check that the first two times are greater than the maximum value
        //         if (lastCollection.Count == lookBackRecord)
        //         {
        //             List<int> averageRequestMillisecondList = new List<int>();//Response time collection
        //             averageRequestMillisecondList.AddRange(lastCollection.Select(z => z.AverageRequestMillisecond).ToList());
        //             averageRequestMillisecondList.Add(currentAverageRequestMS);

        //             for (int i = 0; i < averageRequestMillisecondList.Count; i++)
        //             {
        //                 if (averageRequestMillisecondList[i] <= 0)
        //                 {
        //                     averageRequestMillisecondList[i] = 99999; //If there is no response, it is considered that the response time is too long
        //                 }
        //             }

        //             #region This method has no effect. Since Int is a value type, the assignment here will not affect the value in averageRequestMillisecondList.
        //             //averageRequestMillisecondList.ForEach(z =>
        //             //{
        //             //    if (z <= 0)
        //             //    {
        //             // z = 999999; //No response is considered an infinite response time
        //             //    }
        //             //});
        //             #endregion

        //             //Check whether the maximum heavy rain is only or less than the minimum value
        //             Func<int, bool> checkAboveMaxMillisecond = (max) => averageRequestMillisecondList.Count(ms => ms > max) == averageRequestMillisecondList.Count;
        //             Func<int, bool> checkBelowMinMillisecond = (min) => averageRequestMillisecondList.Count(ms => ms < min) == averageRequestMillisecondList.Count;

        //             if (checkAboveMaxMillisecond(maxMillisecond))
        //             {
        //                 int cutPageCount = 10;//Deduct the number of pages
        //                 int maxMS = 1200;//Maximum milliseconds
        //                 //Judge in 3 levels
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

        //                 punishRecord += "{0} consecutive response times are greater than {1} (milliseconds), {2} pages will be deducted from the number of included pages.<br />".With(averageRequestMillisecondList.Count, maxMS, cutPageCount);

        //                 order.MaxPageCount = Math.Max(1, order.MaxPageCount - cutPageCount);//Decrease N pages
        //                 LogUtility.SitemapLogger.InfoFormat("Order #{0}({1}) has received {2} consecutive response times greater than {3} (milliseconds), and the maximum number of included pages has been deducted by {4} pages"
        //                                                 , order.Id, order.Url, averageRequestMillisecondList.Count, maxMillisecond, cutPageCount);
        //             }
        //             else if (checkBelowMinMillisecond(minMillsecond))
        //             {
        //                 order.MaxPageCount += 2;
        //                 awardRecord += "If the response time is less than {1} (milliseconds) for {0} consecutive times, award {2} pages.<br />".With(lookBackRecord + 1, minMillsecond, 2);

        //                 LogUtility.SitemapLogger.InfoFormat("Order #{0}({1}) has {2} consecutive response times less than {3} (milliseconds), and will be rewarded with 2 maximum page inclusions"
        //                                                 , order.Id, order.Url, averageRequestMillisecondList.Count, minMillsecond);
        //             }
        //         }
        //     }
        // }

        // /// <summary>
        // /// Clean up the automatically generated sitemap and report files, and add statistics to info.text
        // /// </summary>
        // private void CleanStatisticsFiles()
        // {
        //     if (DateTime.Now.Day != 1)
        //     {
        //         return; //Only executed on No. 1
        //     }

        //     LogUtility.SitemapLogger.Info("Start cleaning and counting logs");

        //     var historyDirectories = Directory.GetDirectories(Server.HttpContext.Server.MapPath("~/App_Data/SiteMap/Sitemap.bak/"))
        //                                     .Where(z => Path.GetFileName(z) != DateTime.Now.ToString("yyyy-MM"))//The information of the current month is not cleared
        //                                     .OrderBy(z => z);

        //     foreach (var dir in historyDirectories)
        //     {
        //         var files = Directory.GetFiles(dir);
        //         int filesCount = files.Length;
        //         bool hasInfoFile = files.Count(z => Path.GetFileName(z) == "info.txt") > 0;
        //         if (!hasInfoFile)
        //         {
        //             LogUtility.SitemapLogger.InfoFormat("Create {0}/info.txt", Path.GetFileNameWithoutExtension(dir));

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

        //             //Calculate the number of sitemap.xml files per day
        //             DateTime dt = DateTime.Parse(Path.GetFileNameWithoutExtension(dir) + "-1");//Get the 1st of the current month
        //             int daysInMonth = DateTime.DaysInMonth(dt.Year, dt.Month);//Number of days in this month
        //             Dictionary<int, List<string>> dicFilesInDay = new Dictionary<int, List<string>>();//Category
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
        //                     dicFilesInDay.Last().Value.Add(file);//If it does not exist, add it to the last day (usually occurs at the end of the month, spanning two months)
        //                 }
        //             }
        //             //Output statistical results
        //             string dayXmlCount = string.Join(",", dicFilesInDay.Select(z => z.Value.Count.ToString()).ToArray());
        //             text.AppendLine("DayXmlCount=" + dayXmlCount);

        //             LogUtility.SitemapLogger.InfoFormat("Cleaning of statistics files ended, information:\r\n{0}", text.ToString());
        //             tw.Write(text);
        //             tw.Flush();
        //             tw.Close();

        //             if (totalFileCount != sitemapXmlCount + sitemapHtmlCount + reportCount)
        //             {
        //                 LogUtility.SitemapLogger.ErrorFormat("Error cleaning SItemap history, numbers do not match, please check. Folder: {0}", Path.GetFileName(dir));
        //             }
        //             else
        //             {
        //                 //Accounting is correct, delete redundant files
        //                 try
        //                 {
        //                     LogUtility.SitemapLogger.InfoFormat("Delete automatically generated files. Start.");
        //                     foreach (var delFile in files)
        //                     {
        //                         File.Delete(delFile);
        //                     }
        //                     LogUtility.SitemapLogger.InfoFormat("Delete automatically generated files. End.");
        //                 }
        //                 catch
        //                 {
        //                     LogUtility.SitemapLogger.ErrorFormat("Failed to delete {0} directory file.", Path.GetFileName(dir));
        //                 }
        //                 LogUtility.SitemapLogger.InfoFormat("Cleaning {0} directory completed.", Path.GetFileName(dir));
        //             }
        //         }
        //     }
        //     LogUtility.SitemapLogger.Info("End of cleaning and counting logs");
        // }

        // /// <summary>
        // /// Automatically review Sitemap customization applications
        // /// </summary>
        // private void AutoCheckOrderApply()
        // {
        //     SenparcEntities ctx = new SenparcEntities(Senparc.Xncf.SenMapic.Domain.Config.SenparcDatabaseConfigs.ClientConnectionString);
        //     DateTime dtLimit = DateTime.Now.AddDays(-3);//Search within 3 days
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
        //         //Check if it can be accessed
        //         //UrlData urlData = null;

        //         try
        //         {
        //             SenMapicEngine senMapic = new SenMapicEngine(new string[] { order.Url }, 1, 1, null, null, null, 1, 1, null);
        //             //urlData = senMapic.CrawSingleUrl(order.Url);
        //             var result = senMapic.Build();

        //             if (result.Count > 0 && result.First().Value.Result == 200)
        //             {
        //                 SendEmail sendApplyPassedEmail = new Email.SendEmail(SendEmailType.SiteMap_ApplyPassed);
        //                 //Send application approval notification
        //                 SendEmailParameter_SiteMap_ApplyPassed sendEmailParam = new SendEmailParameter_SiteMap_ApplyPassed(order.UserInfo.Email, order.UserInfo.UserName, order.Id, order.Url);
        //                 sendApplyPassedEmail.Send(sendEmailParam,
        //                                             lineInCache: true,
        //                                             sendImmediately: true,
        //                                             useBackupEmail: true);
        //                 //Modify Order information
        //                 order.InUse = true;//Enable customization
        //                 order.LastCreateTime = order.LastCreateTime.AddDays(-1);//Set to the previous day and automatically collect it during the next polling
        //                 ctx.SaveChanges();

        //                 LogUtility.SitemapLogger.Info("Sitemap customization service automatically approved: (#{0}) {1}".With(order.Id, order.Url));
        //             }
        //             else
        //             {
        //                 LogUtility.SitemapLogger.Error("Sitemap customization service review failed: (#{0}) {1}".With(order.Id, order.Url));
        //                 //TODO: Send email notification
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //             LogUtility.SitemapLogger.Error("Automatic audit customization service exception: (#{0}) {1}"
        //                 .With(order.Id, e.Message), e);
        //         }
        //     }
        // }

        // /// <summary>
        // /// Sitemap automatically updates Callback
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
        //         throw new Exception("URL address cannot match!");
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
    // }
}
