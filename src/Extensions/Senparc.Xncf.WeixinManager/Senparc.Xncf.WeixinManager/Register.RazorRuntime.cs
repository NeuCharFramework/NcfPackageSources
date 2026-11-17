/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.RazorRuntime.cs
    File description: Register.RazorRuntime related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using System.IO;

namespace Senparc.Xncf.WeixinManager
{
	public partial class Register : IXncfRazorRuntimeCompilation  //Legacy comment translated to English.
	{
		#region Legacy section

		public string LibraryPath => Path.Combine(SiteConfig.WebRootPath, "..", "..", "..", "Senparc.Xncf.WeixinManager", "src", "Senparc.Xncf.WeixinManager");

		#endregion
	}
}
