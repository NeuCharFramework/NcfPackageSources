/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FooterContentSanitizer.cs
    文件功能描述：限制 Footer 配置中可渲染的 HTML，仅保留安全链接

    创建标识：Senparc - 20260802

    修改标识：Senparc - 20260804
    修改描述：v0.28.0-preview5 新增数据库升级维护状态与可配置页脚安全处理

----------------------------------------------------------------*/

using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// Footer 内容净化器。普通文本始终编码，仅允许绝对 HTTP/HTTPS 的 a 标签。
    /// </summary>
    public static class FooterContentSanitizer
    {
        private static readonly Regex AnchorRegex = new(
            @"<a\b(?<attributes>[^>]*)>(?<text>.*?)</a\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex HrefRegex = new(
            @"\bhref\s*=\s*(?:""(?<double>[^""]*)""|'(?<single>[^']*)')",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static string Sanitize(string content)
        {
            var source = WebUtility.HtmlDecode(string.IsNullOrWhiteSpace(content)
                ? SystemConfig.CreateDefaultFooterContent()
                : content.Trim());
            var builder = new StringBuilder(source.Length + 32);
            var currentIndex = 0;

            foreach (Match anchorMatch in AnchorRegex.Matches(source))
            {
                AppendEncoded(builder, source.Substring(currentIndex, anchorMatch.Index - currentIndex));

                var hrefMatch = HrefRegex.Match(anchorMatch.Groups["attributes"].Value);
                var href = hrefMatch.Success
                    ? hrefMatch.Groups["double"].Success
                        ? hrefMatch.Groups["double"].Value
                        : hrefMatch.Groups["single"].Value
                    : null;

                if (IsSafeAbsoluteWebUrl(href))
                {
                    builder.Append("<a href=\"")
                        .Append(WebUtility.HtmlEncode(href))
                        .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">");
                    AppendEncoded(builder, StripTags(anchorMatch.Groups["text"].Value));
                    builder.Append("</a>");
                }
                else
                {
                    AppendEncoded(builder, StripTags(anchorMatch.Groups["text"].Value));
                }

                currentIndex = anchorMatch.Index + anchorMatch.Length;
            }

            AppendEncoded(builder, source.Substring(currentIndex));
            return builder.ToString();
        }

        private static bool IsSafeAbsoluteWebUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static string StripTags(string value)
        {
            return Regex.Replace(value ?? string.Empty, "<[^>]*>", string.Empty);
        }

        private static void AppendEncoded(StringBuilder builder, string value)
        {
            builder.Append(WebUtility.HtmlEncode(value ?? string.Empty));
        }
    }
}
