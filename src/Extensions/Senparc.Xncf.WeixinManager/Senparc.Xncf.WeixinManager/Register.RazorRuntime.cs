/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.RazorRuntime.cs
    文件功能描述：Register.RazorRuntime 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
