using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System.IO;

namespace Senparc.Xncf.WeixinManager
{
	public partial class Register : IXncfRazorRuntimeCompilation  //需要使用 RazorRuntimeCompilation，在开发环境下实时更新 Razor Page
	{
		#region IXncfRazorRuntimeCompilation 接口

		public string LibraryPath => Path.Combine(SiteConfig.WebRootPath, "..", "..", "..", "Senparc.Xncf.WeixinManager", "src", "Senparc.Xncf.WeixinManager");

		#endregion
	}
}
