/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FooterContentSanitizer.cs
    File description: Related implementation.

    Created by: Senparc - 20260802

    Modified by: Senparc - 20260804
    Change description: v0.28.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    ///Legacy comment translated to English.
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
