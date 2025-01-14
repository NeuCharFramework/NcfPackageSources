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
                        sb.AppendLine($"# {WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim())}");
                        sb.AppendLine();
                    }

                    // 提取 meta description
                    var descMatch = Regex.Match(text, @"<meta\s+name=[""']description[""']\s+content=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
                    if (descMatch.Success)
                    {
                        sb.AppendLine($"> {WebUtility.HtmlDecode(descMatch.Groups[1].Value.Trim())}");
                        sb.AppendLine();
                    }

                    // 提取 meta keywords
                    var keywordsMatch = Regex.Match(text, @"<meta\s+name=[""']keywords[""']\s+content=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
                    if (keywordsMatch.Success)
                    {
                        var keywords = WebUtility.HtmlDecode(keywordsMatch.Groups[1].Value.Trim());
                        sb.AppendLine($"**关键词**: {keywords}");
                        sb.AppendLine();
                    }

                    // 处理图片
                    text = Regex.Replace(text, 
                        @"<img[^>]+(alt=[""']([^""']*)[""'])?[^>]*(title=[""']([^""']*)[""'])?[^>]*(src=[""']([^""']*)[""'])?[^>]*>",
                        delegate(Match match)
                        {
                            string alt = match.Groups[2].Value.Trim();
                            string title = match.Groups[4].Value.Trim();
                            string src = ResolveUrl(match.Groups[6].Value.Trim());
                            string description = !string.IsNullOrEmpty(alt) ? alt : title;
                            
                            if (!string.IsNullOrEmpty(description))
                            {
                                return $"![{description}]({src})";
                            }
                            return string.Empty;
                        },
                        RegexOptions.IgnoreCase);

                    // 处理链接
                    text = Regex.Replace(text, 
                        @"<a\s[^>]*href=[""']([^""']*)[""'][^>]*(title=[""']([^""']*)[""'])?[^>]*>(.*?)</a>",
                        delegate(Match match)
                        {
                            string href = ResolveUrl(match.Groups[1].Value.Trim());
                            string title = match.Groups[3].Value.Trim();
                            string linkText = match.Groups[4].Value.Trim();
                            
                            if (!string.IsNullOrEmpty(title) && title != linkText)
                            {
                                return $"[{linkText}]({href} \"{title}\")";
                            }
                            return $"[{linkText}]({href})";
                        },
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 处理标题标签
                    text = Regex.Replace(text, @"<h1[^>]*>(.*?)</h1>", "# $1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    text = Regex.Replace(text, @"<h2[^>]*>(.*?)</h2>", "## $1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    text = Regex.Replace(text, @"<h3[^>]*>(.*?)</h3>", "### $1", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 处理段落和换行
                    text = Regex.Replace(text, @"<p[^>]*>(.*?)</p>", "$1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    text = Regex.Replace(text, @"<br[^>]*>", "  \n", RegexOptions.IgnoreCase);

                    // 处理强调
                    text = Regex.Replace(text, @"<strong[^>]*>(.*?)</strong>", "**$1**", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    text = Regex.Replace(text, @"<em[^>]*>(.*?)</em>", "*$1*", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 处理列表
                    text = Regex.Replace(text, @"<li[^>]*>(.*?)</li>", "- $1\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);

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
                    
                    // 处理引用块
                    text = Regex.Replace(text, @"<blockquote[^>]*>(.*?)</blockquote>", "> $1\n\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 处理代码块
                    text = Regex.Replace(text, @"<pre[^>]*><code[^>]*>(.*?)</code></pre>", "```\n$1\n```\n", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    text = Regex.Replace(text, @"<code[^>]*>(.*?)</code>", "`$1`", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 处理表格
                    text = Regex.Replace(text, 
                        @"<table[^>]*>(.*?)</table>",
                        delegate(Match match)
                        {
                            string tableContent = match.Groups[1].Value;
                            var rows = Regex.Matches(tableContent, @"<tr[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                            if (rows.Count == 0) return string.Empty;

                            var sb = new StringBuilder();
                            bool isHeader = true;

                            foreach (Match row in rows)
                            {
                                string rowContent = row.Groups[1].Value;
                                var cells = Regex.Matches(rowContent, @"<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                                
                                if (cells.Count > 0)
                                {
                                    sb.AppendLine("|" + string.Join("|", cells.Cast<Match>().Select(c => c.Groups[1].Value.Trim())) + "|");
                                    
                                    if (isHeader)
                                    {
                                        sb.AppendLine("|" + string.Join("|", Enumerable.Repeat("---", cells.Count)) + "|");
                                        isHeader = false;
                                    }
                                }
                            }
                            return sb.ToString();
                        },
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 处理水平线
                    text = Regex.Replace(text, @"<hr[^>]*>", "---\n", RegexOptions.IgnoreCase);

                    // 处理删除线
                    text = Regex.Replace(text, @"<(del|strike)[^>]*>(.*?)</\1>", "~~$2~~", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 处理下划线
                    text = Regex.Replace(text, @"<u[^>]*>(.*?)</u>", "__$1__", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    // 处理有序列表和无序列表
                    text = Regex.Replace(text, @"<ol[^>]*>(.*?)</ol>", delegate(Match m)
                    {
                        int counter = 1;
                        return Regex.Replace(m.Groups[1].Value,
                            @"<li[^>]*>(.*?)</li>",
                            m2 => $"{counter++}. {m2.Groups[1].Value.Trim()}\n",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    text = Regex.Replace(text, @"<ul[^>]*>(.*?)</ul>", delegate(Match m)
                    {
                        return Regex.Replace(m.Groups[1].Value,
                            @"<li[^>]*>(.*?)</li>",
                            m2 => $"- {m2.Groups[1].Value.Trim()}\n",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    }, RegexOptions.IgnoreCase | RegexOptions.Singleline);

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

        private string ResolveUrl(string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return string.Empty;

            // 如果已经是绝对路径，直接返回
            if (Uri.TryCreate(relativeUrl, UriKind.Absolute, out _))
                return relativeUrl;

            try
            {
                var baseUri = new Uri(Url);
                var absoluteUri = new Uri(baseUri, relativeUrl);
                return absoluteUri.ToString();
            }
            catch
            {
                return relativeUrl;
            }
        }
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
