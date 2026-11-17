/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260724
    Change description: v0.3.0 Updated module behavior and related support.

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Resources;

namespace Senparc.Xncf.Accounts
{
	public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
									IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
	{
		private static readonly ResourceManager AreaResourceManager = new("Senparc.Xncf.Accounts.AccountsResource", typeof(AccountsResource).Assembly);

		private static string AreaT(string key, string fallback)
		{
			return AreaResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
		}

		#region Legacy section

		public string HomeUrl => "/Admin/Account/Index";

		public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
						 new AreaPageMenuItem(GetAreaHomeUrl(), AreaT("Accounts.Home", "首页"),"fa fa-laptop"),
		 			 new AreaPageMenuItem(GetAreaUrl($"/Admin/Account/DatabaseSample"), AreaT("Accounts.DatabaseSample", "数据库操作示例"),"fa fa-bookmark-o")
			 		};

		public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
		{
			builder.AddRazorPagesOptions(options =>
			{
				//configure page permissions here
			});

			SenparcTrace.SendCustomLog("Account 启动", "完成 Area:Senparc.Xncf.Accounts 注册");

			return builder;
		}

#endregion

#region IXncfRazorRuntimeCompilation interface
		public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Accounts"));
#endregion
	}
}
