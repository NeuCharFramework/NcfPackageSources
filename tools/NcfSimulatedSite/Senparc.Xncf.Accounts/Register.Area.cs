/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Area.cs
    文件功能描述：Register.Area 相关实现
    
    
    创建标识：Senparc - 20241028
    
    修改标识：Senparc - 20260717
    修改描述：v0.3.0 为账户模块接入多语言资源与功能文案本地化

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

namespace Senparc.Xncf.Accounts
{
	public partial class Register : IAreaRegister, //注册 XNCF 页面接口（按需选用）
									IXncfRazorRuntimeCompilation  //赋能 RazorPage 运行时编译
	{
		private static string AreaT(string key, string fallback)
		{
			return AccountsResource.Get(key, fallback);
		}

		#region IAreaRegister 接口

		public string HomeUrl => "/Admin/Account/Index";

		public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
						 new AreaPageMenuItem(GetAreaHomeUrl(), AreaT("Accounts.Home", "首页"),"fa fa-laptop"),
		 			 new AreaPageMenuItem(GetAreaUrl($"/Admin/Account/DatabaseSample"), AreaT("Accounts.DatabaseSample", "数据库操作示例"),"fa fa-bookmark-o")
			 		};

		public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
		{
			builder.AddRazorPagesOptions(options =>
			{
				//此处可配置页面权限
			});

			SenparcTrace.SendCustomLog("Account 启动", "完成 Area:Senparc.Xncf.Accounts 注册");

			return builder;
		}

#endregion

#region IXncfRazorRuntimeCompilation 接口
		public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.Accounts"));
#endregion
	}
}
