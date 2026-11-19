/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Area.cs
    File description: Register.Area related implementation

    Created by: Senparc - 20231001

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.17.0-preview5 integrate shared resource localization and improve feature text for the PromptRange module

    Modified by: Senparc - 20261021
    Change description: v0.20.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.20.8 enhance Sandbox functionality with language validation and request handling improvements

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

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

namespace Senparc.Xncf.PromptRange
{
    public partial class Register : IAreaRegister, //register the XNCF page interface (optional)
                                    IXncfRazorRuntimeCompilation  //enable RazorPage runtime compilation
    {
        #region Legacy section

        public string HomeUrl => "/Admin/PromptRange/";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
                         new AreaPageMenuItem(GetAreaUrl(HomeUrl+"Index"), PromptRangeResource.Get("Area.Home", "首页"),"fa fa-laptop"),
                          //new AreaPageMenuItem(GetAreaUrl(HomeUrl+"Model"),"model","fa fa-laptop"),
                           new AreaPageMenuItem(GetAreaUrl(HomeUrl+"Prompt"),"PromptRange","fa fa-laptop"),
                           new AreaPageMenuItem(GetAreaUrl(HomeUrl+"Skill"),"Skill 管理","fa fa-puzzle-piece"),
                          //Legacy comment translated to English.
                     };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.AddRazorPagesOptions(options =>
            {
                //configure page permissions here
            });

            SenparcTrace.SendCustomLog("PromptRange 启动", "完成 Area:Senparc.Xncf.PromptRange 注册");

            return builder;
        }

        #endregion

        #region Legacy section
        public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Xncf.PromptRange"));
        #endregion
    }
}
