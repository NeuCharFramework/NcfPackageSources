/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.RazorRuntime.cs
    文件功能描述：Register.RazorRuntime 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    public partial class Register : IXncfRazorRuntimeCompilation
    {
        public string LibraryPath => Path.Combine(SiteConfig.WebRootPath, "..", "..", "..", "..", "NcfPackageSources", "src", "Extensions", "Senparc.Xncf.DatabaseToolkit");
    }
}
