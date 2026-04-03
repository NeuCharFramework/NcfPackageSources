using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Areas.Admin.SenparcTraceManager
{
    public class SenparcTraceItem
    {
        /// <summary>
        ///title
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// time
        /// </summary>
        public string DateTime { get; set; }
        /// <summary>
        ///thread
        /// </summary>
        public int ThreadId { get; set; }
        /// <summary>
        ///Number of lines in the log
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// log type
        /// </summary>
        public SenparcTraceType SenparcTraceType { get; set; } = SenparcTraceType.Normal;
        /// <summary>
        /// Is it an exception log?
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
        /// Returns the result, usually JSON
        /// </summary>
        public string Result { get; set; }
        public string PostData { get; set; }
        public string TotalResult { get; set; }

        public string ExceptionMessage { get; set; }
        public string ExceptionAccessTokenOrAppId { get; set; }
        public string ExceptionStackTrace { get; set; }

        public override string ToString()
        {
            return TotalResult.Trim().Replace("\r\n", "<br/>");
        }

    }
}
