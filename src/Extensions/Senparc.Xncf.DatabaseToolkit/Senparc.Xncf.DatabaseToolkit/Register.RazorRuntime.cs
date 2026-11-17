/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.RazorRuntime.cs

    File description: Register.RazorRuntime related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
