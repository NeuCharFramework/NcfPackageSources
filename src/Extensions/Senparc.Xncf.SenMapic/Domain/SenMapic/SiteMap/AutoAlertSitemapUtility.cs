/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: AutoAlertSitemapUtility.cs

    File description: AutoAlertSitemapUtility related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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
        //Legacy comment translated to English.
        // /// </summary>
        // public bool BuildOnlyOnce { get; set; }
        // public List<int> SiteMapOrderIds { get; set; }
        // public bool SendEmail { get; set; }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // private string[] notAutoSendUsername = new[] { "ZSU", "ADMIN", "GALIJIKUAI", "ANONYMITY" };
        // private string[] vipUsername = new[] { "ZSU", "ADMIN", "GALIJIKUAI", "ANONYMITY" };
        // private string saveFileDirectory;
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        // private int _autoAlertThreadInUsing = 0;
        // /// <summary>
        //Legacy comment translated to English.
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

        //Legacy comment translated to English.

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
        //Legacy comment translated to English.
        //     }

        //     do
        //     {
        //         _semaphorePool = new Semaphore(maxAutoAlertThread, maxAutoAlertThread);
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //         this.BuildOnlyOnceEventHandler(null, false);
        //Legacy comment translated to English.
        //         _semaphorePool.Close();

        //         if (!BuildOnlyOnce)
        //         {
        //Legacy comment translated to English.
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
            //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.

            //     int listedAllOrdersCount = allQueueOrders.Count;
            //Legacy comment translated to English.

            //Legacy comment translated to English.
            //     if (DateTime.Now.Hour >= 7 && DateTime.Now.Hour <= 21)
            //     {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //         if (listedAllOrdersCount > takeEndCount)
            //         {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //         }
            //     }
            //     else
            //     {
            //Legacy comment translated to English.
            //     }

            //Legacy comment translated to English.

            //     DateTime dtStartWait = DateTime.Now;
            //     foreach (var order in queueOrders)
            //     {
            //         bool successQueuesd = ThreadPool.QueueUserWorkItem(waitCallback, order);
            //         if (successQueuesd)
            //         {
            //Legacy comment translated to English.
            //         }
            //     }

            //Legacy comment translated to English.

            //     while (remainQueueOrderCount > 0)
            //     {
            //         if (dtStartWait.AddMinutes(5) <= DateTime.Now)
            //         {
            //             dtStartWait = DateTime.Now;
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //         }

            //         Thread.Sleep(300);
            //     }

            //Legacy comment translated to English.
            //     if (queueOrders.Count > 0 && (DateTime.Now - dtStartWait).TotalMinutes >= queueOrders.Count * 5)
            //     {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //     }

            //     if (autoAlertThreadInUsing > 0)
            //     {
            //Legacy comment translated to English.
            //     }
            // }
            // catch (Exception e)
            // {
            //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            //     threadStarted = true;
            //     autoAlertThreadInUsing++;

            //     LogUtility.WebLogger.Debug("automaticcollectSitemap:{0} (ID:{1})start".With(order.Url, order.Id.ToString()));

            //     int oldOrderMaxPageCount = order.MaxPageCount;

            //     //collectUrl
            //     string sitemapXmlFileName;
            //     string zipFileName;
            //     string reportFileName;
            //     string sitemapHtmlFileName;
            //     string[] domains = order.Url.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            //     List<string> filterOmitKeywords = order.FilterOmitKeyWords != null ? order.FilterOmitKeyWords.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>();

            //Legacy comment translated to English.
            //     var currentMaxThreadCount = vipUsername.Contains(order.UserName.ToUpper()) ? maxVipBuidThreadCount : maxBuildThreadCount;

            //     SenMapicEngine senMapic = new SenMapicEngine(domains, maxVipBuidThreadCount, 30,
            //         order.Priority, order.Changefreq, DateTime.Now, order.Deep, order.MaxPageCount, filterOmitKeywords);

            //     var totalUrls = senMapic.Build();

            //     BuildGoogleSitemapWithReport sitemapWithReport = new BuildGoogleSitemapWithReport(/*maxVipBuidThreadCount, 30*/);
            //     sitemapWithReport.BuildSitemapReport(order.Url, totalUrls, order.Deep, order.MaxPageCount, order.Priority, order.Changefreq,
            //         DateTime.Now, senMapic.FilterOmitWords, out sitemapXmlFileName, out zipFileName, out reportFileName, out sitemapHtmlFileName);


            //     #region Legacy section
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

            //Legacy comment translated to English.
            //     if (!order.Downloaded.IsNullOrEmpty() && !order.Downloaded.Contains("unlimited"))
            //     {
            //         order.Downloaded = null;//cleardownloadstatusinformation  
            //     }
            //     ctx.SiteMapCollectionSet.Add(siteMapCollection);
            //     #endregion

            //     string awardRecord = null;
            //     string punishRecord = null;
            //     var isVip = vipUsername.Contains(order.UserName.ToUpper());
            //     if (order.MaxPageCount < 2000 && !isVip)
            //     {
            //Legacy comment translated to English.

            //Legacy comment translated to English.
            //         if ((siteMapCollection.SuccessPages + siteMapCollection.FailPages >= oldOrderMaxPageCount)
            //             && oldOrderMaxPageCount > 0 && ((double)siteMapCollection.FailPages / oldOrderMaxPageCount) <= 0.05)
            //         {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //         }

            //Legacy comment translated to English.
            //         AwardOrPunishAverageRequestTime(siteMapCollection, order, ref awardRecord, ref punishRecord);
            //     }
            //     else if (isVip)
            //     {
            //Legacy comment translated to English.
            //         if (order.MaxPageCount < siteMapCollection.SuccessPages * 1.4)
            //         {
            //             order.MaxPageCount = (int)(siteMapCollection.SuccessPages * 1.4);
            //         }

            //Legacy comment translated to English.
            //     }

            //     ctx.SaveChanges();

            //Legacy comment translated to English.

            //     siteMapCollection = ctx.SiteMapCollectionSet.FirstOrDefault(z => z.Guid == siteMapCollection.Guid);

            //     #region save files
            //     //save files
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
            //Legacy comment translated to English.
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

            //             LogUtility.SitemapLogger.InfoFormat("Sitemap Callbacksuccess：#{0}，Url：{1},Callback：{2}", order.Id, order.Url, order.Callback);
            //         }
            //         catch (Exception e)
            //         {
            //             LogUtility.SitemapLogger.Error("Sitemap Callbackerror：" + e.Message, e);
            //         }
            //     }

            //     #endregion

            //     #region sendEmail
            //     if (SendEmail)
            //     {
            //Legacy comment translated to English.
            //         if (awardRecord.IsNullOrEmpty())
            //         {
            //             awardRecord = "none";
            //         }
            //         if (punishRecord.IsNullOrEmpty())
            //         {
            //             punishRecord = "none";
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
            //Legacy comment translated to English.
            //             sendEmail.Send(sendEmailParam, true, sendImmediately, true);
            //         }
            //         else
            //         {
            //Legacy comment translated to English.
            //         }
            //     }
            //     #endregion

            //Legacy comment translated to English.
            // }
            // catch (Exception e)
            // {
            //     try
            //     {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //     }
            //     catch (Exception ex)
            //     {
            //Legacy comment translated to English.
            //     }

            //     //AutoSendLogEmail.SendLogEmail(e);
            // }
            // finally
            // {
            //     //LogUtility.WebLogger.Debug("automaticcollectSitemap:{0}sendEmailcomplete".With(order.Url));
            //     semaphorePoolPreviousCount = _semaphorePool.Release();
            //     remainQueueOrderCount--;
            //     if (threadStarted)
            //     {
            //         autoAlertThreadInUsing--;
            //     }
            // }
        // }

        /// <summary>
        ///Legacy comment translated to English.
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
            //Legacy comment translated to English.
            //     }

            //Legacy comment translated to English.
            //     if (currentLoginTime.Add(tsAllowMaxUnloginTime + TimeSpan.FromDays(15)) < order.LastCreateTime)
            //     {
            //Legacy comment translated to English.
            //         SendEmail sendAlertEmail = new SendEmail(SendEmailType.SiteMap_Remove);
            //         var sendEmailParam = new SendEmailParameter_SiteMap_Remove(order.UserInfo.Email, order.UserName, order.Id, order.Url);
            //         sendAlertEmail.Send(sendEmailParam, true, true, true);

            //         //deletefile
            //         SiteMapHandler siteMapHandler = new SiteMapHandler();
            //         siteMapHandler.RemoveSiteMapCollectionFiles(order);

            //Legacy comment translated to English.
            //         ctx.SiteMapOrderSet.Remove(order);

            //         ctx.SaveChanges();
            //Legacy comment translated to English.
            //     }
            //     else if (currentLoginTime.Add(tsAllowMaxUnloginTime + TimeSpan.FromDays(5)) < order.LastCreateTime)
            //     {
            //Legacy comment translated to English.
            //         SendEmail sendAlertEmail = new SendEmail(SendEmailType.SiteMap_LoginExpired);
            //         var sendEmailParam = new SendEmailParameter_SiteMap_LoginExpired(order.UserInfo.Email, order.UserName, order.UserInfo.CurrentLoginTime, order.Id, order.Url);
            //         sendAlertEmail.Send(sendEmailParam, true, true, true);

            //         order.InUse = false;
            //         ctx.SaveChanges();
            //     }
            // }
        }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        //Legacy comment translated to English.
        // /// <param name="order">Order</param>
        // private void AwardOrPunishAverageRequestTime(SiteMapCollection siteMapCollection, SiteMapOrder order, ref string awardRecord, ref string punishRecord)
        // {
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //     int currentAverageRequestMS = siteMapCollection.AverageRequestMillisecond;
        //     if (((currentAverageRequestMS > maxMillisecond || currentAverageRequestMS == 0) && order.MaxPageCount > 1)
        //         || currentAverageRequestMS < minMillsecond)
        //     {
        //Legacy comment translated to English.
        //         var lastCollection = ctx.SiteMapCollectionSet.Include("SiteMapOrder").Where(z => z.SiteMapOrder.Id == order.Id).OrderByDescending(z => z.Id).Take(lookBackRecord).ToList();

        //Legacy comment translated to English.
        //         if (lastCollection.Count == lookBackRecord)
        //         {
        //Legacy comment translated to English.
        //             averageRequestMillisecondList.AddRange(lastCollection.Select(z => z.AverageRequestMillisecond).ToList());
        //             averageRequestMillisecondList.Add(currentAverageRequestMS);

        //             for (int i = 0; i < averageRequestMillisecondList.Count; i++)
        //             {
        //                 if (averageRequestMillisecondList[i] <= 0)
        //                 {
        //Legacy comment translated to English.
        //                 }
        //             }

        //             #region Legacy section
        //             //averageRequestMillisecondList.ForEach(z =>
        //             //{
        //             //    if (z <= 0)
        //             //    {
        //Legacy comment translated to English.
        //             //    }
        //             //});
        //             #endregion

        //Legacy comment translated to English.
        //             Func<int, bool> checkAboveMaxMillisecond = (max) => averageRequestMillisecondList.Count(ms => ms > max) == averageRequestMillisecondList.Count;
        //             Func<int, bool> checkBelowMinMillisecond = (min) => averageRequestMillisecondList.Count(ms => ms < min) == averageRequestMillisecondList.Count;

        //             if (checkAboveMaxMillisecond(maxMillisecond))
        //             {
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
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

        //Legacy comment translated to English.

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //                                                 , order.Id, order.Url, averageRequestMillisecondList.Count, maxMillisecond, cutPageCount);
        //             }
        //             else if (checkBelowMinMillisecond(minMillsecond))
        //             {
        //                 order.MaxPageCount += 2;
        //Legacy comment translated to English.

        //Legacy comment translated to English.
        //                                                 , order.Id, order.Url, averageRequestMillisecondList.Count, minMillsecond);
        //             }
        //         }
        //     }
        // }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // private void CleanStatisticsFiles()
        // {
        //     if (DateTime.Now.Day != 1)
        //     {
        //Legacy comment translated to English.
        //     }

        //Legacy comment translated to English.

        //     var historyDirectories = Directory.GetDirectories(Server.HttpContext.Server.MapPath("~/App_Data/SiteMap/Sitemap.bak/"))
        //Legacy comment translated to English.
        //                                     .OrderBy(z => z);

        //     foreach (var dir in historyDirectories)
        //     {
        //         var files = Directory.GetFiles(dir);
        //         int filesCount = files.Length;
        //         bool hasInfoFile = files.Count(z => Path.GetFileName(z) == "info.txt") > 0;
        //         if (!hasInfoFile)
        //         {
        //             LogUtility.SitemapLogger.InfoFormat("create{0}/info.txt", Path.GetFileNameWithoutExtension(dir));

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

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        //                 }
        //             }
        //             //outputstatisticsresult
        //             string dayXmlCount = string.Join(",", dicFilesInDay.Select(z => z.Value.Count.ToString()).ToArray());
        //             text.AppendLine("DayXmlCount=" + dayXmlCount);

        //Legacy comment translated to English.
        //             tw.Write(text);
        //             tw.Flush();
        //             tw.Close();

        //             if (totalFileCount != sitemapXmlCount + sitemapHtmlCount + reportCount)
        //             {
        //Legacy comment translated to English.
        //             }
        //             else
        //             {
        //Legacy comment translated to English.
        //                 try
        //                 {
        //Legacy comment translated to English.
        //                     foreach (var delFile in files)
        //                     {
        //                         File.Delete(delFile);
        //                     }
        //Legacy comment translated to English.
        //                 }
        //                 catch
        //                 {
        //                     LogUtility.SitemapLogger.ErrorFormat("delete{0}directoryfilefailure。", Path.GetFileName(dir));
        //                 }
        //Legacy comment translated to English.
        //             }
        //         }
        //     }
        //Legacy comment translated to English.
        // }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // private void AutoCheckOrderApply()
        // {
        //     SenparcEntities ctx = new SenparcEntities(Senparc.Xncf.SenMapic.Domain.Config.SenparcDatabaseConfigs.ClientConnectionString);
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        //         //UrlData urlData = null;

        //         try
        //         {
        //             SenMapicEngine senMapic = new SenMapicEngine(new string[] { order.Url }, 1, 1, null, null, null, 1, 1, null);
        //             //urlData = senMapic.CrawSingleUrl(order.Url);
        //             var result = senMapic.Build();

        //             if (result.Count > 0 && result.First().Value.Result == 200)
        //             {
        //                 SendEmail sendApplyPassedEmail = new Email.SendEmail(SendEmailType.SiteMap_ApplyPassed);
        //Legacy comment translated to English.
        //                 SendEmailParameter_SiteMap_ApplyPassed sendEmailParam = new SendEmailParameter_SiteMap_ApplyPassed(order.UserInfo.Email, order.UserInfo.UserName, order.Id, order.Url);
        //                 sendApplyPassedEmail.Send(sendEmailParam,
        //                                             lineInCache: true,
        //                                             sendImmediately: true,
        //                                             useBackupEmail: true);
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //                 ctx.SaveChanges();

        //Legacy comment translated to English.
        //             }
        //             else
        //             {
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //Legacy comment translated to English.
        //                 .With(order.Id, e.Message), e);
        //         }
        //     }
        // }

        // /// <summary>
        // /// SitemapautomaticupdateCallback
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
        //Legacy comment translated to English.
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
