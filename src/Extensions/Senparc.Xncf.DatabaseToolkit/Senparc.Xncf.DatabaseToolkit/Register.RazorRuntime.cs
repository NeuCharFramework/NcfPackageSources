using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit
{
    public partial class Register : IXscfRazorRuntimeCompilation
    {
        public string LibraryPath => Path.Combine(SiteConfig.WebRootPath, "..", "..", "..", "..", "ScfPackageSources", "src", "Extensions", "Senparc.Xncf.DatabaseToolkit");
    }
}
