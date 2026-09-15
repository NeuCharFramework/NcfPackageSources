/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Area.cs
    文件功能描述：Register.Area 相关实现


    创建标识：Senparc - 20250105

    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.3.0-preview3 为 KnowledgeBase 模块接入统一资源本地化并优化功能文案

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview8 完善知识库文件删除保护、召回测试与管理界面

    修改标识：Senparc - 20260915
    修改描述：v0.7.2 优化知识库模块注册与管理端状态展示

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
            // HomeUrl 与知识库管理页相同，保留一个真实入口即可。
            new AreaPageMenuItem(GetAreaUrl($"/Admin/KnowledgeBase/Index"), KnowledgeBaseResource.Get("Area.KnowledgeBase.Management", "知识库管理"),"fa fa-bookmark-o"),
            new AreaPageMenuItem(GetAreaUrl($"/Admin/KnowledgeBase/RecallTest"), KnowledgeBaseResource.Get("Area.KnowledgeBase.RecallTest", "召回测试"),"fa fa-bookmark-o"),
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
            // 开发时优先挂载物理 wwwroot，避免 cshtml 已热更新而 JS/CSS 仍读旧内嵌资源导致 Vue 白屏。
            try
            {
                var candidates = new List<string>();
                if (!string.IsNullOrWhiteSpace(LibraryPath))
                {
                    candidates.Add(Path.Combine(LibraryPath, "wwwroot"));
                }
                var asmLocation = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrWhiteSpace(asmLocation))
                {
                    // bin/Debug/netX.Y → 项目根/wwwroot
                    candidates.Add(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(asmLocation)!, "..", "..", "..", "wwwroot")));
                }

                foreach (var candidate in candidates)
                {
                    var physicalWwwroot = Path.GetFullPath(candidate);
                    if (!Directory.Exists(physicalWwwroot))
                    {
                        continue;
                    }
                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(physicalWwwroot)
                    });
                    SenparcTrace.SendCustomLog("KnowledgeBase 静态资源", "已挂载物理 wwwroot：" + physicalWwwroot);
                    break;
                }
            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("KnowledgeBase 静态资源", "物理 wwwroot 挂载失败：" + ex.Message);
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot"),
            });

            return base.UseXncfModule(app, registerService);
        }

        #endregion

        #region IXncfRazorRuntimeCompilation 接口
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.KnowledgeBase"));
        #endregion
    }
}
