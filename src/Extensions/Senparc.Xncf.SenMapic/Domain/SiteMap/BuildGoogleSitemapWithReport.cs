using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Senparc.Xncf.SenMapic.Domain.Models;

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


//             urls = (string.IsNullOrEmpty(urls) || !urls.StartsWith("http")) ? "http://" + HttpContext.Current.Request.Url.Host : urls.Trim();//当格式错误或为空时，使用当前域名
//             List<string> urlList = urls.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

//             for (int i = 0; i < urlList.Count; i++)
//             {
//                 _domainAll += (i == 0 ? "" : "+") + this.GetDomain(urlList[i]);//所有域名
//             }
//             _domainAll = _domainAll.Replace(":", ",").Replace("/", "");

//             //指定储存目录
//             string saveDirectory = string.Format(GlobalConfigString.SITEMAP_SAVE_FILE_PATH + "/Sitemap.bak/{0}", DateTime.Now.ToString("yyyy-MM"));
//             //指定文件名
//             _sitemapXmlFileName = FileSaveUtility.GetAvailableFileName(Path.Combine(Server.GetMapPath(saveDirectory), "sitemap-{0}-{1}.xml".With(_domainAll, DateTime.Now.ToString("yyyyMMdd"))));
//             sitemapXmlFileName = _sitemapXmlFileName;
//             //查看储存文件目录是否存在，并赋予权限
//             string physicalDirPath = Server.GetMapPath(saveDirectory);//物理路径
//             if (!Directory.Exists(physicalDirPath))
//             {
//                 Directory.CreateDirectory(physicalDirPath);//如果目录不存在则创建
//                 IoUtility.AddDirectorySecurity(physicalDirPath, "NETWORK SERVICE", System.Security.AccessControl.FileSystemRights.FullControl, System.Security.AccessControl.AccessControlType.Allow);
//             }

//             //创建sitemap.xml
//             BuildSiteMapXML(totalUrl, updateDate, priority, changefreq, saveDirectory);
//             //创建sitemap.html和report.html
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
//             //    LogUtility.SitemapLogger.Error("BuildGoogleSitemapWithReport出错:{0}".With(e.Message, e));
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
//             //组成XML文件
//             XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
//             XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
//             XElement root = new XElement(xmlns + "urlset");
//             root.SetAttributeValue(XNamespace.Xmlns + "xsi", xsi);
//             root.SetAttributeValue(xsi + "schemaLocation",
// @"http://www.sitemaps.org/schemas/sitemap/0.9  http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd");//取消了换行
//             foreach (var urlData in totalUrls.Where(z => z.Value.Result == (int)HttpStatusCode.OK))
//             {
//                 XElement urlElement = new XElement(xmlns + "url",
//                                             new XElement(xmlns + "loc", this.EscapingUrl(urlData.Value.Url)));
//                 //页面更新时间
//                 if (updateDate != null)
//                 {
//                     urlElement.Add(new XElement(xmlns + "lastmod",
//                        string.Format("{0}+{1}:00", ((DateTime)updateDate).ToString("s"), TimeZone.CurrentTimeZone.GetUtcOffset((DateTime)updateDate).TotalHours.ToString("00"))));
//                 }
//                 //优先级
//                 if (!string.IsNullOrEmpty(priority))
//                 {
//                     urlElement.Add(new XElement(xmlns + "priority", priority));
//                 }
//                 //更新频率
//                 if (!string.IsNullOrEmpty(changefreq))
//                 {
//                     urlElement.Add(new XElement(xmlns + "changefreq", changefreq));
//                 }
//                 root.Add(urlElement);
//             }

//             //保存文件完整路径（包含文件名）
//             //如果文件已存在，则生成一个新的文件名
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

//             reportFileName = FileSaveUtility.GetAvailableFileName(_sitemapXmlFileName + "-report.html");//如果文件已存在，则生成一个新的文件名
//             sitemapHtmlFileName = FileSaveUtility.GetAvailableFileName(_sitemapXmlFileName + "-sitemap.html");//如果文件已存在，则生成一个新的文件名

//             try
//             {
//                 //读取Report模板
//                 using (var srTemplate = new StreamReader(Server.GetMapPath("~/App_Data/Template/SitemapReport.htm"), Encoding.UTF8))
//                 {
//                     _reportTemplate = srTemplate.ReadToEnd();
//                 }
//                 using (StreamWriter rwReportHtml = System.IO.File.CreateText(reportFileName))//创建文件
//                 {
//                     //输入数据
//                     this.InsertReportTemplateValue("version", Config.SystemParameters.SenparcGoogleSitemapVersion);

//                     this.InsertReportTemplateValue("filename", Path.GetFileName(_sitemapXmlFileName));
//                     this.InsertReportTemplateValue("htmlfilename", Path.GetFileName(sitemapHtmlFileName));

//                     this.InsertReportTemplateValue("version", SystemParameters.SenparcGoogleSitemapVersion);
//                     this.InsertReportTemplateValue("buildtime", DateTime.Now.ToString());
//                     this.InsertReportTemplateValue("longdate", DateTime.Now.ToLongDateString());
//                     this.InsertReportTemplateValue("url", urls);
//                     this.InsertReportTemplateValue("deep", maxDeep.ToString());
//                     this.InsertReportTemplateValue("frequent", changefreq != "none" ? changefreq : "不设置");
//                     this.InsertReportTemplateValue("updatetime", updateDate != null ? DateTime.Now.ToString() : "不设置");
//                     this.InsertReportTemplateValue("priority", priority != "none" ? priority : "不设置");
//                     this.InsertReportTemplateValue("frequent", priority != "none" ? priority : "不设置");

//                     int totalPageCount = totalUrls.Count;
//                     int successPageCount = totalUrls.Count(z => z.Value.Result == (int)HttpStatusCode.OK);
//                     int failedPageCount = totalPageCount - successPageCount;
//                     this.InsertReportTemplateValue("totalpagecount", totalPageCount.ToString());
//                     this.InsertReportTemplateValue("successpagecount", successPageCount.ToString());
//                     this.InsertReportTemplateValue("failpagecount", failedPageCount.ToString());
//                     this.InsertReportTemplateValue("responsetime", averageRequestTime.TotalMilliseconds.ToString("###,###"));
//                     this.InsertReportTemplateValue("totalpagesizekb", totalPageSizeKB.ToString("###,###.###"));

//                     //过滤关键字
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
//                         filterWords.Append("无");
//                     }
//                     this.InsertReportTemplateValue("filteromitkeywords", filterWords.ToString());

//                     StringBuilder failPageReport = new StringBuilder();
//                     if (failedPageCount > 0)
//                     {
//                         failPageReport.Append("<table class=\"datatable\"><tr><th>错误页面URL</th><th>错误类型</th><th>错误页面父页URL</th></tr>");

//                         foreach (var urlData in totalUrls.Where(z => z.Value.Result != (int)HttpStatusCode.OK))
//                         {
//                             failPageReport.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>", urlData.Value.Url, urlData.Value.Result.ToString(), urlData.Value.ParentUrl);
//                         }
//                         failPageReport.Append("</table>");
//                     }
//                     else
//                     {
//                         failPageReport.Append("<span style=\"color:#f00\">恭喜！无错误页面！</span>");
//                     }
//                     this.InsertReportTemplateValue("errorpages", failPageReport.ToString());

//                     rwReportHtml.Write(this._reportTemplate);
//                     rwReportHtml.Flush();
//                     rwReportHtml.Close();
//                 }

//                 //生成sitemap.html
//                 //读取Html模板
//                 using (var srTemplate = new StreamReader(Server.GetMapPath("~/App_Data/Template/SitemapHtml.htm"), Encoding.UTF8))
//                 {
//                     _sitemapHtmlTemplate = srTemplate.ReadToEnd();
//                 }
//                 using (StreamWriter rwSitemapHtml = System.IO.File.CreateText(sitemapHtmlFileName))//创建文件
//                 {
//                     //输入数据
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
//                     //TODO:整个过程有时候会执行几个小时！！持续观察！（如http://www.shoestog.com）
//                 }

//                 //记录统计数据
//                 sitemapBuildStatistic.SiteCount += totalDomainsCount;
//                 sitemapBuildStatistic.PageCount += totalUrls.Count;
//                 sitemapBuildStatistic.PageSizeKB += totalPageSizeKB;
//                 xmlCtx.Save(new SitemapBuildStatistic[] { sitemapBuildStatistic });
//             }
//             catch (Exception e)
//             {
//                 SenparcTrace.SendCustomLog("SiteMap", $"BuildGoogleSitemapWithReport出错:{e.Message}", e);
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
//         /// 获取域名，如www.senparc.com
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

//         #region 替换模板值
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
//                 throw new Exception("报表模板未载入！");
//             }

//             template = template.Replace("{$" + valueName.ToLower() + "$}", value);
//         }
//         #endregion
    // }
}
