﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System;
using Senparc.Ncf.XncfBase;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Senparc.CO2NET.RegisterServices;
using System.Reflection;

namespace Senparc.Xncf.KnowledgeBase
{
    public partial class Register : IAreaRegister, //注册 XNCF 页面接口（按需选用）
                                    IXncfRazorRuntimeCompilation  //赋能 RazorPage 运行时编译
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/KnowledgeBase/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
new AreaPageMenuItem(GetAreaHomeUrl(),"首页","fa fa-laptop"),
new AreaPageMenuItem(GetAreaUrl($"/Admin/KnowledgeBases/Index"),"知识库管理new","fa fa-bookmark-o"),
new AreaPageMenuItem(GetAreaUrl($"/Admin/KnowledgeBase/KnowledgeBase"),"知识库管理","fa fa-bookmark-o"),
new AreaPageMenuItem(GetAreaUrl("/Admin/KnowledgeBase/RetrievalTest"),"召回测试","fa fa-bookmark-o"),
		};

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //此处可配置页面权限
            });

            SenparcTrace.SendCustomLog("KnowledgeBase 启动", "完成 Area:Senparc.Xncf.KnowledgeBase 注册");

            return builder;
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });

            return base.UseXncfModule(app, registerService);
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.KnowledgeBase"));
        #endregion
    }
}
