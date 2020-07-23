using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// 方法返回结果
    /// </summary>
    public class FunctionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// 日志
        /// </summary>
        public string Log { get; set; }
        /// <summary>
        /// 消息结果
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 异常信息（此时 Success 一般为 false）
        /// </summary>
        public XscfFunctionException Exception { get; set; }
    }
}
