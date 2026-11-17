/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.7.2 Updated module behavior and related support.

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
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

        public string HomeUrl => "/Admin/KnowledgeBase/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
            //Legacy comment translated to English.
            new AreaPageMenuItem(GetAreaUrl($"/Admin/KnowledgeBase/Index"), KnowledgeBaseResource.Get("Area.KnowledgeBase.Management", "知识库管理"),"fa fa-bookmark-o"),
            new AreaPageMenuItem(GetAreaUrl($"/Admin/KnowledgeBase/RecallTest"), KnowledgeBaseResource.Get("Area.KnowledgeBase.RecallTest", "召回测试"),"fa fa-bookmark-o"),
        };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("KnowledgeBase 启动", "完成 Area:Senparc.Xncf.KnowledgeBase 注册");

            return builder;
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
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

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.KnowledgeBase"));
        #endregion
    }
}
