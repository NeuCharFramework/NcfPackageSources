/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileGenerateResult.cs
    文件功能描述：FileGenerateResult 相关实现
    
    
    创建标识：Senparc - 20231003
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
