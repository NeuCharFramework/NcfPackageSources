/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BuildGoogleSitemapWithReport.cs
    File description: BuildGoogleSitemapWithReport related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
//     public class BuildGoogleSitemapWithReport
//     {
//         //private int _maxThread;
//         //private int _maxBuildMinutes;
//         private string _reportTemplate;
//         private string _sitemapHtmlTemplate;
//         private string _sitemapXmlFileName;
//         private string _domainAll;
//         //public BuildGoogleSitemapWithReport() : this(-1) { }
//         public BuildGoogleSitemapWithReport(/*int maxThread, int maxBuildMinutes*/)
//         {
//             //_maxThread = maxThread; _maxBuildMinutes = maxBuildMinutes;
//         }

//         public void BuildSitemapReport(string urls, Dictionary<string,UrlData> totalUrl, int deep, int maxPageCount, string priority, string changefreq,
//          DateTime? updateDate, List<FilterOmitWord> filterOmitKeyWords,
//         out string sitemapXmlFileName, out string zipFileName, out string reportFileName, out string sitemapHtmlFileName)
//         {
//             //try
//             //{


//Legacy comment translated to English.
//             List<string> urlList = urls.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

//             for (int i = 0; i < urlList.Count; i++)
//             {
//Legacy comment translated to English.
//             }
//             _domainAll = _domainAll.Replace(":", ",").Replace("/", "");

//Legacy comment translated to English.
//             string saveDirectory = string.Format(GlobalConfigString.SITEMAP_SAVE_FILE_PATH + "/Sitemap.bak/{0}", DateTime.Now.ToString("yyyy-MM"));
//Legacy comment translated to English.
//             _sitemapXmlFileName = FileSaveUtility.GetAvailableFileName(Path.Combine(Server.GetMapPath(saveDirectory), "sitemap-{0}-{1}.xml".With(_domainAll, DateTime.Now.ToString("yyyyMMdd"))));
//             sitemapXmlFileName = _sitemapXmlFileName;
//Legacy comment translated to English.
//Legacy comment translated to English.
//             if (!Directory.Exists(physicalDirPath))
//             {
//Legacy comment translated to English.
//                 IoUtility.AddDirectorySecurity(physicalDirPath, "NETWORK SERVICE", System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow);
//             }

//             //createsitemap.xml
//             BuildSiteMapXML(totalUrl, updateDate, priority, changefreq, saveDirectory);
//             //createsitemap.htmlandreport.html
//             var averageTime = totalUrl.Count == 0 ? 0 : totalUrl.Values.Average(z => z.ResponseMillionSeconds);
//             BuildReport(
//                 urls: urls,
//                 totalDomainsCount: urlList.Count,
//                 totalUrls: totalUrl,
//                 averageRequestTime: TimeSpan.FromMilliseconds(averageTime),
//                 totalPageSizeKB: totalUrl.Sum(z => z.Value.SizeKB),
//                 filterOmitWords: filterOmitKeyWords,
//                 priority: priority,
//                 changefreq: changefreq,
//                 updateDate: updateDate,
//                 maxDeep: deep,
//                 maxPageCount: maxPageCount,
//                 zipFileName: out zipFileName,
//                 reportFileName: out reportFileName,
//                 sitemapHtmlFileName: out sitemapHtmlFileName);


//             //}
//             //catch (Exception e)
//             //{
//Legacy comment translated to English.
//             //    senMapic = null;
//             //    zipFileName=null;
//             //    reportFileName=null;
//             //    sitemapHtmlFileName=null;
//             //}

//         }

//         private void BuildSiteMapXML(Dictionary<string, UrlData> totalUrls, DateTime? updateDate, string priority, string changefreq, string saveDirectory)
//         {
//             //try
//             //{
//Legacy comment translated to English.
//             XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
//             XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
//             XElement root = new XElement(xmlns + "urlset");
//             root.SetAttributeValue(XNamespace.Xmlns + "xsi", xsi);
//             root.SetAttributeValue(xsi + "schemaLocation",
//Legacy comment translated to English.
//             foreach (var urlData in totalUrls.Where(z => z.Value.Result == (int)HttpStatusCode.OK))
//             {
//                 XElement urlElement = new XElement(xmlns + "url",
//                                             new XElement(xmlns + "loc", this.EscapingUrl(urlData.Value.Url)));
//Legacy comment translated to English.
//                 if (updateDate != null)
//                 {
//                     urlElement.Add(new XElement(xmlns + "lastmod",
//                        string.Format("{0}+{1}:00", ((DateTime)updateDate).ToString("s"), TimeZone.CurrentTimeZone.GetUtcOffset((DateTime)updateDate).TotalHours.ToString("00"))));
//                 }
//Legacy comment translated to English.
//                 if (!string.IsNullOrEmpty(priority))
//                 {
//                     urlElement.Add(new XElement(xmlns + "priority", priority));
//                 }
//Legacy comment translated to English.
//                 if (!string.IsNullOrEmpty(changefreq))
//                 {
//                     urlElement.Add(new XElement(xmlns + "changefreq", changefreq));
//                 }
//                 root.Add(urlElement);
//             }

//Legacy comment translated to English.
//Legacy comment translated to English.
//             var fileSavePath = FileSaveUtility.GetAvailableFileName(_sitemapXmlFileName);
//             root.Save(fileSavePath);
//         }


//         private void BuildReport(string urls, int totalDomainsCount, Dictionary<string, UrlData> totalUrls,
//             TimeSpan averageRequestTime, double totalPageSizeKB, List<FilterOmitWord> filterOmitWords,
//             string priority, string changefreq, DateTime? updateDate, int maxDeep, int maxPageCount,
//             out string zipFileName, out string reportFileName, out string sitemapHtmlFileName)
//         {
//             XmlDataContext xmlCtx = new XmlDataContext();
//             SitemapBuildStatistic sitemapBuildStatistic = xmlCtx.GetXmlList<SitemapBuildStatistic>()[0];

//             zipFileName = $"sitemap-{urls.Replace("http://", "+").Replace("https://", "+").Replace(":", ",")}-{DateTime.Now:yyyy-MM-dd}.zip";

//Legacy comment translated to English.
//Legacy comment translated to English.

//             try
//             {
//Legacy comment translated to English.
//                 using (var srTemplate = new StreamReader(Server.GetMapPath("~/App_Data/Template/SitemapReport.htm"), Encoding.UTF8))
//                 {
//                     _reportTemplate = srTemplate.ReadToEnd();
//                 }
//                 using (StreamWriter rwReportHtml = System.IO.File.CreateText(reportFileName))//create files
//                 {
//                     //inputdata
//                     this.InsertReportTemplateValue("version", Config.SystemParameters.SenparcGoogleSitemapVersion);

//                     this.InsertReportTemplateValue("filename", Path.GetFileName(_sitemapXmlFileName));
//                     this.InsertReportTemplateValue("htmlfilename", Path.GetFileName(sitemapHtmlFileName));

//                     this.InsertReportTemplateValue("version", SystemParameters.SenparcGoogleSitemapVersion);
//                     this.InsertReportTemplateValue("buildtime", DateTime.Now.ToString());
//                     this.InsertReportTemplateValue("longdate", DateTime.Now.ToLongDateString());
//                     this.InsertReportTemplateValue("url", urls);
//                     this.InsertReportTemplateValue("deep", maxDeep.ToString());
//                     this.InsertReportTemplateValue("frequent", changefreq != "none" ? changefreq : "notsettings");
//                     this.InsertReportTemplateValue("updatetime", updateDate != null ? DateTime.Now.ToString() : "notsettings");
//                     this.InsertReportTemplateValue("priority", priority != "none" ? priority : "notsettings");
//                     this.InsertReportTemplateValue("frequent", priority != "none" ? priority : "notsettings");

//                     int totalPageCount = totalUrls.Count;
//                     int successPageCount = totalUrls.Count(z => z.Value.Result == (int)HttpStatusCode.OK);
//                     int failedPageCount = totalPageCount - successPageCount;
//                     this.InsertReportTemplateValue("totalpagecount", totalPageCount.ToString());
//                     this.InsertReportTemplateValue("successpagecount", successPageCount.ToString());
//                     this.InsertReportTemplateValue("failpagecount", failedPageCount.ToString());
//                     this.InsertReportTemplateValue("responsetime", averageRequestTime.TotalMilliseconds.ToString("###,###"));
//                     this.InsertReportTemplateValue("totalpagesizekb", totalPageSizeKB.ToString("###,###.###"));

//Legacy comment translated to English.
//                     StringBuilder filterWords = new StringBuilder();
//                     if (filterOmitWords != null && filterOmitWords.Count > 0)
//                     {
//                         foreach (var item in filterOmitWords)
//                         {
//                             filterWords.AppendFormat("{0}：{1}<br />", FilterOmitWord.OperatorKinds[(int)item.Operatorkind], item.Keyword.ToLower());
//                         }
//                     }
//                     else
//                     {
//                         filterWords.Append("none");
//                     }
//                     this.InsertReportTemplateValue("filteromitkeywords", filterWords.ToString());

//                     StringBuilder failPageReport = new StringBuilder();
//                     if (failedPageCount > 0)
//                     {
//Legacy comment translated to English.

//                         foreach (var urlData in totalUrls.Where(z => z.Value.Result != (int)HttpStatusCode.OK))
//                         {
//                             failPageReport.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>", urlData.Value.Url, urlData.Value.Result.ToString(), urlData.Value.ParentUrl);
//                         }
//                         failPageReport.Append("</table>");
//                     }
//                     else
//                     {
//Legacy comment translated to English.
//                     }
//                     this.InsertReportTemplateValue("errorpages", failPageReport.ToString());

//                     rwReportHtml.Write(this._reportTemplate);
//                     rwReportHtml.Flush();
//                     rwReportHtml.Close();
//                 }

//                 //generatesitemap.html
//Legacy comment translated to English.
//                 using (var srTemplate = new StreamReader(Server.GetMapPath("~/App_Data/Template/SitemapHtml.htm"), Encoding.UTF8))
//                 {
//                     _sitemapHtmlTemplate = srTemplate.ReadToEnd();
//                 }
//                 using (StreamWriter rwSitemapHtml = System.IO.File.CreateText(sitemapHtmlFileName))//create files
//                 {
//                     //inputdata
//                     this.InsertSitemapTemplateValue("url", urls);
//                     this.InsertSitemapTemplateValue("version", Config.SystemParameters.SenparcGoogleSitemapVersion);

//                     StringBuilder sitemapLinksHtml = new StringBuilder();
//                     foreach (var item in totalUrls.Where(z => z.Value.Result == (int)HttpStatusCode.OK))
//                     {
//                         sitemapLinksHtml.AppendFormat("<li><a href=\"{0}\">{1}</a></li>\r\n", item.Value.Url, item.Value.Title.IsNullOrEmpty() ? "&nbsp;" : item.Value.Title);
//                     }
//                     this.InsertSitemapTemplateValue("links", sitemapLinksHtml.ToString());
//                     this.InsertSitemapTemplateValue("now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

//                     rwSitemapHtml.Write(this._sitemapHtmlTemplate);
//                     rwSitemapHtml.Flush();
//                     rwSitemapHtml.Close();
//Legacy comment translated to English.
//                 }

//                 //recordstatisticsdata
//                 sitemapBuildStatistic.SiteCount += totalDomainsCount;
//                 sitemapBuildStatistic.PageCount += totalUrls.Count;
//                 sitemapBuildStatistic.PageSizeKB += totalPageSizeKB;
//                 xmlCtx.Save(new SitemapBuildStatistic[] { sitemapBuildStatistic });
//             }
//             catch (Exception e)
//             {
//Legacy comment translated to English.
//             }
//         }


//         private string EscapingUrl(string url)
//         {
//             return url.Replace("&amp", "&").Replace("&", "&amp")
//                       .Replace("\"", "&apos")
//                       .Replace(">", "&gt")
//                       .Replace("<", "&lt");
//         }

//         /// <summary>
//Legacy comment translated to English.
//         /// </summary>
//         /// <param name="url"></param>
//         /// <returns></returns>
//         private string GetDomain(string url)
//         {
//             var reg = Regex.Match(url, "(?<=http(s)?://)([^/]+)", RegexOptions.IgnoreCase);
//             if (reg.Success)
//             {
//                 return reg.Value;
//             }
//             return null;
//         }

//         #region replace template values
//         private void InsertReportTemplateValue(string valueName, string value)
//         {
//             InsertTemplateValue(ref this._reportTemplate, valueName, value);
//         }

//         private void InsertSitemapTemplateValue(string valueName, string value)
//         {
//             InsertTemplateValue(ref this._sitemapHtmlTemplate, valueName, value);
//         }

//         private void InsertTemplateValue(ref string template, string valueName, string value)
//         {
//             if (template.IsNullOrEmpty())
//             {
//Legacy comment translated to English.
//             }

//             template = template.Replace("{$" + valueName.ToLower() + "$}", value);
//         }
//         #endregion
    // }
}
