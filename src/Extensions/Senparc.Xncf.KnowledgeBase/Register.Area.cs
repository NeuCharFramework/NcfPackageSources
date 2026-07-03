/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Area.cs
    文件功能描述：Register.Area 相关实现
    
    
    创建标识：Senparc - 20250105
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Config;
using System;
using System.Collections.Generic;
using System.IO;
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
            new AreaPageMenuItem(GetAreaUrl($"/Admin/KnowledgeBase/Index"),"知识库管理","fa fa-bookmark-o"),
            new AreaPageMenuItem(GetAreaUrl($"/Admin/KnowledgeBase/RecallTest"),"召回测试","fa fa-bookmark-o"),
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

            //var staticResourceSetting = app.ApplicationServices.GetService<IOptionsMonitor<StaticResourceSetting>>();

            ////静态资源允许跨域
            //var path = Path.Combine(Directory.GetCurrentDirectory(), staticResourceSetting.CurrentValue.RootDir);
            //if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            //var fileOptions = new StaticFileOptions()
            //{
            //    FileProvider = new PhysicalFileProvider(path),
            //    RequestPath = staticResourceSetting.CurrentValue.RequestPath,
            //    OnPrepareResponse = (x) =>//验证静态资源授权
            //    {
            //        //var token = x.Context.Request.Query["token"];
            //        //if (string.IsNullOrWhiteSpace(token))
            //        //    token = x.Context.Request.Headers["token"];
            //        //try
            //        //{
            //        //    x.Context.RequestServices.GetService<SysKeyService>().ValidToken(token).GetAwaiter().GetResult();
            //        //}
            //        //catch (Exception ex)
            //        //{
            //        //    x.Context.Response.StatusCode = StatusCodes.Status404NotFound;
            //        //    x.Context.Response.WriteAsync(ex.Message);
            //        //}
            //        //new StatusCodes().Status401Unauthorized
            //        //x.Context.Response.Headers.Add("Access-Control-Allow-Origin", "*");//允许跨域，Core已做处理
            //    }
            //};
            //app.UseStaticFiles(fileOptions);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot"),
                //RequestPath = staticResourceSetting.CurrentValue.RequestPath,
            });

            return base.UseXncfModule(app, registerService);
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.KnowledgeBase"));
        #endregion
    }
}
