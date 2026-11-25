/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: SenparcTraceItem.cs
    File description: SenparcTraceItem related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Areas.Admin.SenparcTraceManager
{
    public class SenparcTraceItem
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string DateTime { get; set; }
        /// <summary>
        /// thread
        /// </summary>
        public int ThreadId { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public SenparcTraceType SenparcTraceType { get; set; } = SenparcTraceType.Normal;
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsException { get; set; }
        /// <summary>
        /// result
        /// </summary>
        public WeicinTraceItemContent Result { get; set; } = new WeicinTraceItemContent();

        public string ResultStr => Result?.ToString();

    }

    public class WeicinTraceItemContent
    {
        public string AccessTokenOrAppId { get; set; }
        public string Url { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Result { get; set; }
        public string PostData { get; set; }
        public string TotalResult { get; set; }

        public string ExceptionMessage { get; set; }
        public string ExceptionAccessTokenOrAppId { get; set; }
        public string ExceptionStackTrace { get; set; }

        public override string ToString()
        {
            return (TotalResult ?? string.Empty).Trim();
        }

    }

    public class SenparcTraceLogFileItem
    {
        public string Date { get; set; }
        public long FileSize { get; set; }
        public DateTimeOffset LastModified { get; set; }
    }

    public class SenparcTraceLogFileResult
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<SenparcTraceLogFileItem> Items { get; set; } = new();
    }

    public class SenparcTraceLogListItem
    {
        public int No { get; set; }
        public int Line { get; set; }
        public string DateTime { get; set; }
        public int ThreadId { get; set; }
        public SenparcTraceType SenparcTraceType { get; set; }
        public bool IsException { get; set; }
        public string Title { get; set; }
        public string Preview { get; set; }
        public bool HasDetail { get; set; }
    }

    public class SenparcTraceLogQueryResult
    {
        public string Date { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int ExceptionCount { get; set; }
        public long FileSize { get; set; }
        public List<SenparcTraceLogListItem> Items { get; set; } = new();
        public List<int> Threads { get; set; } = new();
        public Dictionary<SenparcTraceType, int> TypeCounts { get; set; } = new();
    }

    public class SenparcTraceLogDetail
    {
        public int Line { get; set; }
        public string DateTime { get; set; }
        public int ThreadId { get; set; }
        public SenparcTraceType SenparcTraceType { get; set; }
        public bool IsException { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Result { get; set; }
        public string PostData { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionStackTrace { get; set; }
        public string TotalResult { get; set; }
    }
}
