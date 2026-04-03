using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// When you need to use the AddRazorRuntimeCompilation() method, you need to set the path corresponding to the current project relative to Senparc.Web
    /// </summary>
    public interface IXncfRazorRuntimeCompilation
    {
        /// <summary>
        /// Relative path, such as: Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Areas.Admin"));
        /// </summary>
        string LibraryPath { get; }
    }
}
