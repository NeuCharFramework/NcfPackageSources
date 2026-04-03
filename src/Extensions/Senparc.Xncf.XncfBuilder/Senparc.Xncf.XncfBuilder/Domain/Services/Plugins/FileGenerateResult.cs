using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Plugins
{
    /// <summary>
    ///File generation results
    /// </summary>
    public class FileGenerateResult
    {
        /// <summary>
        /// filename (may also include path)
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// code or file content
        /// </summary>
        public string EntityCode { get; set; }
    }
}
