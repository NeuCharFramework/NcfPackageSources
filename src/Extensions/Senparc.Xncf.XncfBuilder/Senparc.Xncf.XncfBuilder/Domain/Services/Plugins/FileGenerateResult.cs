using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Plugins
{
    /// <summary>
    /// 文件生成结果
    /// </summary>
    public class FileGenerateResult
    {
        /// <summary>
        /// 文件名（可能会同时包含路径）
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 代码或文件内容
        /// </summary>
        public string EntityCode { get; set; }
    }
}
