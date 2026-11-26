/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PagerBarExtension.cs
    File description: PagerBarExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260731
    Change description: v0.22.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core;
using Senparc.Ncf.Utility;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace System.Web.Mvc
{
    /// <summary>
    /// Identifies the CSS theme used by the legacy pager renderer.
    /// </summary>
    public enum BarStyle
    {
        defaultStyle = 0,//default
        digg,
        yahoo,
        meneame,
        flickr,
        sabrosus,
        scott,
        quotes,
        black,
        black2,
        black_red,
        grayr,
        yellow,
        jogger,
        starcraft2,
        tres,
        megas512,
        technorati,
        youtube,
        msdn,
        badoo,
        manu,
        green_black,
        viciao,
        yahoo2,
        weixinShopPager,
        taskPager,
        bootstrap
    }

    /// <summary>
    /// Carries page coordinates and total-record metadata for pager rendering.
    /// </summary>
    public class PagerSocureData
    {
        /// <summary>Gets or sets the current one-based page index.</summary>
        public int PageIndex { get; set; }
        /// <summary>Gets or sets the number of records per page.</summary>
        public int PageCount { get; set; }
        /// <summary>Gets or sets the total number of matching records.</summary>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Stores the text, navigation, layout, and paging options used by
    /// <see cref="PagerBarExtension"/>.
    /// </summary>
    public class PagerBarSettings
    {
        public BarStyle barStyle { get; set; }

        public string Text { get; set; }

        public int PageCount { get; set; }

        public int PageIndex { get; set; }

        public int TotalCount { get; set; }

        public int ShowPageNumber { get; set; }

        public string NoRecordTip { get; set; }

        public string PreWord { get; set; }

        public string NextWord { get; set; }

        public string Onclick { get; set; }

        public string Url { get; set; }

        /// <summary>Gets or sets whether generated links target an iframe context.</summary>
        public bool InIframe { get; set; }

        public string BarMark { get; set; }

        public string CurrentPageWordFormat { get; set; }

        public bool ShowTotalCount { get; set; }

        /// <summary>Initializes settings with the default page size and first page.</summary>
        public PagerBarSettings() : this(null) { }

        /// <summary>
        /// Initializes settings with a navigation URL.
        /// </summary>
        /// <param name="url">The base URL used by generated page links.</param>
        public PagerBarSettings(string url) : this(null, 0, 0, url, false) { }

        /// <summary>
        /// Initializes settings with a navigation URL and iframe target mode.
        /// </summary>
        /// <param name="url">The base URL used by generated page links.</param>
        /// <param name="inIframe">Whether links target an iframe context.</param>
        public PagerBarSettings(string url, bool inIframe) : this(null, 0, 0, url, inIframe) { }

        /// <summary>
        /// Initializes settings with explicit page coordinates and navigation target options.
        /// </summary>
        /// <param name="pageIndex">The one-based current page; defaults to 1 when null.</param>
        /// <param name="pageCount">The page size; defaults to 20 when non-positive.</param>
        /// <param name="totalCount">The total number of matching records.</param>
        /// <param name="url">The base URL used by generated page links.</param>
        /// <param name="inIframe">Whether links target an iframe context.</param>
        public PagerBarSettings(int? pageIndex, int pageCount, int totalCount, string url, bool inIframe)
        {
            this.PageIndex = pageIndex ?? 1;
            this.PageCount = pageCount > 0 ? pageCount : 20;
            this.TotalCount = totalCount;
            this.Url = url;
            this.InIframe = inIframe;

            this.ShowTotalCount = true;
        }
    }


    public static class PagerBarExtension
    {
        /// <summary>
        /// Renders pagination controls from a paged entity result using default settings.
        /// </summary>
        /// <typeparam name="T">The source entity type.</typeparam>
        /// <param name="html">The HTML helper used for view context.</param>
        /// <param name="dataSource">The paged source data.</param>
        /// <returns>Pagination HTML.</returns>
        public static string PagerBar<T>(this IHtmlHelper html, PagedList<T> dataSource) where T : class
        {
            return PagerBar(html, dataSource, null);
        }

        /// <summary>
        /// Renders pagination controls from a paged entity result.
        /// </summary>
        /// <typeparam name="T">The source entity type.</typeparam>
        /// <param name="html">The HTML helper used for view context.</param>
        /// <param name="dataSource">The paged source data.</param>
        /// <param name="settings">Optional rendering settings.</param>
        /// <returns>Pagination HTML.</returns>
        public static string PagerBar<T>(this IHtmlHelper html, PagedList<T> dataSource, PagerBarSettings settings) where T : class
        {
            settings = settings ?? new PagerBarSettings();
            settings.PageIndex = dataSource.PageIndex;
            settings.PageCount = dataSource.PageCount;
            settings.TotalCount = dataSource.TotalCount;
            return PagerBar(html, settings);
        }


        /// <summary>
        /// Renders pagination controls from generic pager source data.
        /// </summary>
        /// <typeparam name="T">The compatibility type parameter.</typeparam>
        /// <param name="html">The HTML helper used for view context.</param>
        /// <param name="dataSource">The pager source data.</param>
        /// <param name="settings">Optional rendering settings.</param>
        /// <returns>Pagination HTML.</returns>
        public static string PagerBar<T>(this IHtmlHelper html, PagerSocureData dataSource, PagerBarSettings settings) where T : class
        {
            settings = settings ?? new PagerBarSettings();
            settings.PageIndex = dataSource.PageIndex;
            settings.PageCount = dataSource.PageCount;
            settings.TotalCount = dataSource.TotalCount;
            return PagerBar(html, settings);
        }



        /// <summary>
        /// Renders pagination markup after applying default values and page boundaries.
        /// </summary>
        /// <param name="html">The HTML helper used for view context.</param>
        /// <param name="settings">The pagination settings to normalize and render.</param>
        /// <returns>Pagination HTML.</returns>
        public static string PagerBar(this IHtmlHelper html, PagerBarSettings settings)
        {
            SetDefaultValue(settings);//Legacy comment translated to English.

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<div class=\"{0}\" >", settings.barStyle.ToString());

            //checkemptyrecord
            if (settings.TotalCount == 0)
            {
                sb.Append(settings.NoRecordTip);
                sb.Append("</div>");

                return sb.ToString();
            }

            int totalPage = Convert.ToInt32((settings.TotalCount - 1) / settings.PageCount) + 1;//Legacy comment translated to English.

            string backPageStyle = (settings.PageIndex <= 1) ? "disabled" : "";
            string nextPageStyle = (settings.PageIndex >= totalPage) ? "disabled" : "";

            var firstDisplayPageEnd = 0;//Legacy comment translated to English.
            var bodyDisplayPageStart = 0;//Legacy comment translated to English.
            var bodyDisplayPageEnd = 0;//Legacy comment translated to English.
            var endDisplayPageStart = 0;//Legacy comment translated to English.


            //Legacy comment translated to English.
            bodyDisplayPageStart = (settings.PageIndex - settings.ShowPageNumber <= 1) ? 1 : settings.PageIndex - settings.ShowPageNumber; // (ViewData.pageIndex - ViewData.showPageNumber <= ViewData.showPageNumber) ? ViewData.showPageNumber + 1 : ViewData.pageIndex - ViewData.showPageNumber;


            //Legacy comment translated to English.
            bodyDisplayPageEnd = (settings.PageIndex + settings.ShowPageNumber >= totalPage) ? totalPage : settings.PageIndex + settings.ShowPageNumber;


            //Legacy comment translated to English.
            if (bodyDisplayPageStart > 1)
            {
                if (bodyDisplayPageStart - settings.ShowPageNumber <= 1)
                {
                    firstDisplayPageEnd = bodyDisplayPageStart - 1;
                }
                else
                {
                    firstDisplayPageEnd = settings.ShowPageNumber;
                }
            }
            else
            {
                firstDisplayPageEnd = 0;
            }

            //Legacy comment translated to English.
            if (bodyDisplayPageEnd < totalPage)
            {
                if (bodyDisplayPageEnd + settings.ShowPageNumber >= totalPage)
                {
                    endDisplayPageStart = bodyDisplayPageEnd + 1;
                }
                else
                {
                    endDisplayPageStart = totalPage - settings.ShowPageNumber + 1;
                }
            }
            else
            {
                endDisplayPageStart = totalPage + 1;
            }
            //Legacy comment translated to English.


            //startoutput

            //Legacy comment translated to English.
            if (settings.PageIndex <= 1)
            {
                sb.Append("<span class=\"").Append(backPageStyle).Append("\">").Append(settings.PreWord).Append("</span>");
            }
            else
            {
                sb.Append(GetPageLink(settings.PageIndex - 1, settings.PageIndex, settings.PreWord, settings.CurrentPageWordFormat, settings.Onclick, settings.Url, settings.BarMark, settings.InIframe));
            }


            //first
            for (var i = 1; i <= firstDisplayPageEnd; i++)
            {
                sb.Append(GetPageLink(i, settings.PageIndex, i.ToString(), settings.CurrentPageWordFormat, settings.Onclick, settings.Url, settings.BarMark, settings.InIframe));
            }

            //Legacy comment translated to English.
            if (firstDisplayPageEnd + 1 < bodyDisplayPageStart)
            {
                sb.Append("<span>... </span>");
            }

            //body
            for (var i = bodyDisplayPageStart; i <= bodyDisplayPageEnd; i++)
            {
                sb.Append(GetPageLink(i, settings.PageIndex, i.ToString(), settings.CurrentPageWordFormat, settings.Onclick, settings.Url, settings.BarMark, settings.InIframe));
            }

            //Legacy comment translated to English.
            if (bodyDisplayPageEnd + 1 < endDisplayPageStart)
            {
                sb.Append("<span>... </span>");
            }

            //end
            for (var i = endDisplayPageStart; i <= totalPage; i++)
            {
                sb.Append(GetPageLink(i, settings.PageIndex, i.ToString(), settings.CurrentPageWordFormat, settings.Onclick, settings.Url, settings.BarMark, settings.InIframe));
            }

            //Legacy comment translated to English.
            if (settings.PageIndex >= totalPage)
            {
                sb.Append("<span class=\"").Append(nextPageStyle).Append("\">").Append(settings.NextWord).Append("</span>");
            }
            else
            {
                sb.Append(GetPageLink(settings.PageIndex + 1, settings.PageIndex, settings.NextWord, settings.CurrentPageWordFormat, settings.Onclick, settings.Url, settings.BarMark, settings.InIframe));
            }

            //Legacy comment translated to English.
            if (settings.ShowTotalCount)
            {
                int recordStart = (settings.PageIndex - 1) * settings.PageCount + 1;
                int recordEnd = recordStart + settings.PageCount - 1;
                if (recordEnd > settings.TotalCount)
                {
                    recordEnd = settings.TotalCount;//Legacy comment translated to English.
                }
                var displayedRange = recordStart == recordEnd ? recordStart.ToString() : (recordStart + "-" + recordEnd);
                var currentText = string.Format(NcfCoreResource.Get("Pager.Current", "当前显示第{0}条"), displayedRange);
                var totalText = string.Format(NcfCoreResource.Get("Pager.Total", "总共 <b>{0}</b> 条"), settings.TotalCount);
                sb.Append("<span class=\"total\" title=\"").Append(currentText).Append("\">").Append(totalText).Append("</span>");
            }

            sb.Append("</div>");

            return sb.ToString();
        }

        /// <summary>
        /// Applies default style, page, text, and page-size values to pagination settings.
        /// </summary>
        /// <param name="settings">The mutable settings object.</param>
        private static void SetDefaultValue(PagerBarSettings settings)
        {
            if (settings.barStyle == BarStyle.defaultStyle)
            {
                settings.barStyle = BarStyle.quotes;
            }

            if (settings.PageIndex == 0)
            {
                settings.PageIndex = 1;
            }

            if (settings.ShowPageNumber == 0)
            {
                settings.ShowPageNumber = 3;
            }

            if (string.IsNullOrEmpty(settings.PreWord))
            {
                settings.PreWord = NcfCoreResource.Get("Pager.Previous", "上一页");
            }

            if (string.IsNullOrEmpty(settings.NextWord))
            {
                settings.NextWord = NcfCoreResource.Get("Pager.Next", "下一页");
            }

            if (settings.PageCount == 0)
            {
                settings.PageCount = 20;
            }

            if (string.IsNullOrEmpty(settings.CurrentPageWordFormat))
            {
                settings.CurrentPageWordFormat = "{0}";
            }

        }

        /// <summary>
        /// Builds one current-page span or navigation anchor.
        /// </summary>
        /// <param name="linkPageIndex">The target page index.</param>
        /// <param name="currentPageIndex">The selected page index.</param>
        /// <param name="text">The link text.</param>
        /// <param name="currentPageWordFormat">The page text format.</param>
        /// <param name="onclick">The optional JavaScript handler.</param>
        /// <param name="url">The optional URL template.</param>
        /// <param name="barMark">The fragment marker for non-navigating links.</param>
        /// <param name="inInframe">Whether to add the iframe data attribute.</param>
        /// <returns>The generated page link markup.</returns>
        private static string GetPageLink(int linkPageIndex, int currentPageIndex, string text, string currentPageWordFormat, string onclick, string url, string barMark, bool inInframe)
        {
            //Legacy comment translated to English.

            onclick = (onclick != null) ? "onclick=\"" + onclick + "\"" : "";
            onclick = onclick.Replace("{pageindex}", linkPageIndex.ToString());

            var httpContext = SenparcDI.GetServiceProvider().GetService<IHttpContextAccessor>().HttpContext;

            url = (!string.IsNullOrEmpty(url)) ? url.UrlDecode().Replace(currentPageWordFormat, linkPageIndex.ToString()) : "";
            var href = (onclick != null && onclick.IndexOf("return false") != -1) ? "href=\"#" + barMark + "\" " : "href=\"" + url + "\" ";

            var linkHTML = "";

            string formatedText = currentPageWordFormat.IndexOf("{0}") >= 0 ? string.Format(currentPageWordFormat, text) : text;//Legacy comment translated to English.

            if (linkPageIndex == currentPageIndex)
            {
                linkHTML = $"<span class=\"current\">{formatedText}</span>";
            }
            else
            {
                linkHTML = "<a " + href + onclick + $" {(inInframe ? "data - iniframe =\"1\"" : null)}>{text}</a>";
            }
            return linkHTML;
        }
    }
}
