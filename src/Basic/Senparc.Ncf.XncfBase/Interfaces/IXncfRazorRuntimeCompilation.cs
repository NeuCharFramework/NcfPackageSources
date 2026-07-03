/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IXncfRazorRuntimeCompilation.cs
    文件功能描述：IXncfRazorRuntimeCompilation 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// 需要使用 AddRazorRuntimeCompilation() 方法时，需要设置对应当前项目相对于 Senparc.Web 的路径
    /// </summary>
    public interface IXncfRazorRuntimeCompilation
    {
        /// <summary>
        /// 相对路径，如：Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Areas.Admin"));
        /// </summary>
        string LibraryPath { get; }
    }
}
