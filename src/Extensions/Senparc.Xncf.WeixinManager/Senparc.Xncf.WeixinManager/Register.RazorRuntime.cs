using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System.IO;

namespace Senparc.Xncf.WeixinManager
{
	public partial class Register : IXncfRazorRuntimeCompilation  //Need to use RazorRuntimeCompilation to update Razor Page in real time in the development environment
	{
		#region IXncfRazorRuntimeCompilation 接口

		public string LibraryPath => Path.Combine(SiteConfig.WebRootPath, "..", "..", "..", "Senparc.Xncf.WeixinManager", "src", "Senparc.Xncf.WeixinManager");

		#endregion
	}
}
