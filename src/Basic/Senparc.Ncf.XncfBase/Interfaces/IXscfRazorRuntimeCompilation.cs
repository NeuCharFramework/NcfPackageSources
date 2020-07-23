using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// 需要使用 AddRazorRuntimeCompilation() 方法时，需要设置对应当前项目相对于 Senparc.Web 的路径
    /// </summary>
    public interface IXscfRazorRuntimeCompilation
    {
        /// <summary>
        /// 相对路径，如：Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Areas.Admin"));
        /// </summary>
        string LibraryPath { get; }
    }
}
