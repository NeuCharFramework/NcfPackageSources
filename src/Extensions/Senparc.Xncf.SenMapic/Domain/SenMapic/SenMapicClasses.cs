using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace Senparc.Xncf.SenMapic.Domain.SiteMap
{
    using Senparc.CO2NET.Extensions;
    using System.Threading;

    /// <summary>
    /// URL包含HTML内容信息，及爪录统计结果
    /// </summary>
    public class UrlData
    {
        private string _title;
        public string Url { get; set; }
        public int Deep { get; set; }
        public string TitleHtml { get; set; }
        private string _html;
        public string Html 
        { 
            get => _html;
            set
            {
                _html = value;
                if (!string.IsNullOrEmpty(value))
                {
                    var text = value;
                    var sb = new StringBuilder();

                    // 提取标题
                    var titleMatch = Regex.Match(text, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (titleMatch.Success)
                    {
                        sb.AppendLine($"标题: {WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim())}");
                    }

                    // 提取 meta description
                    var descMatch = Regex.Match(text, @"<meta\s+name=[""']description[""']\s+content=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
                    if (descMatch.Success)
                    {
                        sb.AppendLine($"描述: {WebUtility.HtmlDecode(descMatch.Groups[1].Value.Trim())}");
                    }

                    // 提取 meta keywords
                    var keywordsMatch = Regex.Match(text, @"<meta\s+name=[""']keywords[""']\s+content=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
                    if (keywordsMatch.Success)
                    {
                        sb.AppendLine($"关键词: {WebUtility.HtmlDecode(keywordsMatch.Groups[1].Value.Trim())}");
                    }

                    sb.AppendLine("正文内容:");
                    
                    // 移除 script、style 标签及内容
                    text = Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
                    text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
                    
                    // 移除注释
                    text = Regex.Replace(text, @"<!--[\s\S]*?-->", "", RegexOptions.IgnoreCase);
                    
                    // 移除 header、footer、nav、广告相关区域
                    text = Regex.Replace(text, @"<(header|footer|nav|aside)[^>]*>[\s\S]*?</\1>", "", RegexOptions.IgnoreCase);
                    
                    // 移除常见无意义内容区域
                    text = Regex.Replace(text, @"<div[^>]*(class|id)=['""]?(advertisement|comment|sidebar|footer|header|menu|nav)['""]\s*[^>]*>[\s\S]*?</div>", "", RegexOptions.IgnoreCase);
                    
                    // 移除其他HTML标签，但保留一些有语义的换行
                    text = Regex.Replace(text, @"<(br|p|div|h[1-6])[^>]*>", "\n", RegexOptions.IgnoreCase);
                    text = Regex.Replace(text, "<[^>]+>", "");
                    
                    // 处理特殊字符
                    text = WebUtility.HtmlDecode(text);
                    
                    // 清理空白字符
                    text = Regex.Replace(text, @"[\s\r\n]+", " ").Trim();
                    
                    sb.Append(text);
                    HtmlText = sb.ToString();
                }
                else
                {
                    HtmlText = null;
                }
            }
        }
        
        /// <summary>
        /// 存储不带HTML标记的纯文本内容
        /// </summary>
        public string HtmlText { get; private set; }
        public int Result { get; set; }
        public double SizeKB { get; set; }
        public string ParentUrl { get; set; }
        public int ResponseMillionSeconds { get; set; }
        //public DateTime CreateTime { get; set; }
        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(_title))
                {
                    return _title;
                }
                else
                {
                    if (string.IsNullOrEmpty(TitleHtml))
                    {
                        return "";
                    }
                    //原来是：@"(?<=<head[.\w\W\s\S]*>[.\w\W\s\S]*<title[.\w\W\s\S]*>)([.\w\W\s\S][^</]*)(?=</title>)"
                    //第二版本：@"(?<=<head[.\w\W\s\S]*>[.\w\W\s\S]*<title[.\w\W\s\S]*>)([.\w\W\s\S][^<]*)(?=</title>)"
                    Regex regex = new Regex(@"(?<=<title[.\w\W\s\S]*>)([.\w\W\s\S][^<]*)(?=</title>)", RegexOptions.IgnoreCase);
                    _title = regex.Match(TitleHtml).Value.Trim().Replace("\r\n", "").HtmlEncode();
                    TitleHtml = null;//清空TitleHtml
                    return _title;
                }
            }
        }

        public UrlData(string url, int deep, string html, string titleHtml, int result, double sizeKB, string parentUrl, int responseMillionSeconds)
        {
            Url = url;
            Deep = deep;
            Html = html;
            TitleHtml = titleHtml;
            Result = result;
            SizeKB = sizeKB;
            ParentUrl = parentUrl;
            ResponseMillionSeconds = responseMillionSeconds;
            //CreateTime = DateTime.Now;
        }

        public UrlData(string url, int deep, string html, string titleHtml, int result, double sizeKB, string parentUrl)
            : this(url, deep, html, titleHtml, result, sizeKB, parentUrl, 0) { }

    }

    /// <summary>
    /// 分层搜索Url集合
    /// </summary>
    public class UrlPathCollection
    {
        public string Url { get; set; }
        public int Deep { get; set; }
        public List<string> SearchedUrl { get; set; }
        public UrlPathCollection(string url, int deep, List<string> searchedUrl)
        {
            Url = url;
            Deep = deep;
            SearchedUrl = searchedUrl;
        }
    }

    /// <summary>
    /// Url状态
    /// </summary>
    public enum AvailableUrlStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        UnStart,
        /// <summary>
        /// 进行中
        /// </summary>
        Started,
        /// <summary>
        /// 已结束
        /// </summary>
        Finished,
        /// <summary>
        /// 搜索下一层
        /// </summary>
        Digged
    }

    /// <summary>
    /// 可使用Url
    /// </summary>
    public class AvailableUrl
    {
        public string Url { get; set; }
        public string ParentUrl { get; set; }
        public int Deep { get; set; }
        public AvailableUrlStatus Status { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime StartBuildTime { get; set; }

        public AvailableUrl(string url, string parentUrl, int deep, AvailableUrlStatus status)
        {
            Url = url; ParentUrl = parentUrl; Deep = deep; Status = status;
            CreateTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 过滤关键字
    /// </summary>
    public class FilterOmitWord
    {
        public static readonly string[] OperatorKinds = new[] { "未知", "整个URL包含关键字", "URL参数中包含关键字", "以关键字开头", "以关键字结尾", "指定完整URL" };
        /// <summary>
        /// 过滤关键字类型
        /// </summary>
        public enum OperatorKind
        {
            None,
            Contains,
            ContainsParameters,
            StartWith,
            EndWith,
            Equal
        }

        public string Keyword { get; set; }
        public OperatorKind Operatorkind { get; set; }
        public FilterOmitWord(string keyword, OperatorKind operatorkind)
        {
            Keyword = keyword.ToUpper().Trim();
            Operatorkind = operatorkind;
        }
    }

    /// <summary>
    /// 当前正在被爬行的Url集合
    /// </summary>
    public class CurrentCrawlingUrlList : Dictionary<string, AvailableUrl>, IDictionary<string, AvailableUrl>
    {
        private object syncLockCurrentCrawlingUrlList = new object();

        new public AvailableUrl this[string key]
        {
            get
            {
                lock (syncLockCurrentCrawlingUrlList)
                {
                    return base[key];
                }
            }
            set
            {
                lock (syncLockCurrentCrawlingUrlList)
                {
                    base[key] = value;
                }
            }
        }

        /// <summary>
        /// 判断是否包含Key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        new public bool ContainsKey(string key)
        {
            lock (syncLockCurrentCrawlingUrlList)
            {
                return base.ContainsKey(key);
            }
        }

        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        new public void Remove(string key)
        {
            if (this.ContainsKey(key))
            {
                lock (syncLockCurrentCrawlingUrlList)
                {
                    base.Remove(key);
                }
            }
        }


    }

    /// <summary>
    /// 模拟系统Semaphore(为兼容Silverlight)
    /// </summary>
    //public class SenMapicSemaphore
    //{
    //    private int count;
    //    private int max;

    //    public SenMapicSemaphore(int max)
    //    {
    //        this.max = max;
    //    }

    //    public void WaitOne()
    //    {
    //        while (count >= max)
    //        {
    //            Thread.Sleep(50);
    //        }
    //        count++;
    //    }

    //    public void Release()
    //    {
    //        if (count >= 0)
    //        {
    //            count--;
    //        }
    //    }
    //}
}
