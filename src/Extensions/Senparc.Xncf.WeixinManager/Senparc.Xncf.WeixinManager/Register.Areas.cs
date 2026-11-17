/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Areas.cs
    File description: Register.Areas related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.23.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260729
    Change description: v0.23.1-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.ApiBind;
using Senparc.CO2NET.Utilities;
using Senparc.CO2NET.WebApi.WebApiEngines;
using Senparc.Ncf.Core.Areas;

namespace Senparc.Xncf.WeixinManager
{
    public partial class Register : IAreaRegister //register the XNCF page interface (optional)
    {
        #region Legacy section

        public string HomeUrl => "/Admin/WeixinManager/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
             new AreaPageMenuItem(GetAreaUrl("/Admin/WeixinManager/Index"), WeixinManagerResource.Get("Area.Home", "首页"),"fa fa-laptop"),
             new AreaPageMenuItem(GetAreaUrl("/swagger"), WeixinManagerResource.Get("Area.ApiDocumentation", "Web API Swagger"),"fa fa-file-code-o"),
             new AreaPageMenuItem(GetAreaUrl("/Admin/WeixinManager/MpAccount"), WeixinManagerResource.Get("Area.WeixinManager.OfficialAccounts", "公众号管理"),"fa fa-comments"),
             new AreaPageMenuItem(GetAreaUrl("/Admin/WeixinManager/WeixinClaw"), "个人微信 Bot","fa fa-wechat"),
             new AreaPageMenuItem(GetAreaUrl("/Admin/WeixinManager/WeixinUser"), WeixinManagerResource.Get("Area.WeixinManager.Users", "用户管理"),"fa fa-users"),
        };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {

            var services = builder.Services;
            //WebApiEngineExtensions.WebApiInitFinished = false;
            //Legacy comment translated to English.
            services.AddAndInitDynamicApi(builder, options =>
            {
                options.DefaultRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get;
                options.DocXmlPath = ServerUtility.ContentRootMapPath("~/App_Data/ApiDocXml");
                options.BaseApiControllerType = null;
                options.CopyCustomAttributes = true;
                options.TaskCount = Environment.ProcessorCount * 4;
                // Detailed API logging can capture request parameters such as
                // app secrets and access tokens. Restrict it to development.
                options.ShowDetailApiLog = env.IsDevelopment();
                options.AdditionalAttributeFunc = null;
                // The generated SDK endpoints are not a public, authenticated API
                // surface. Keep them local-only unless a deployment explicitly
                // introduces a separate authentication and allow-list boundary.
                options.ForbiddenExternalAccess = true;
                options.UseLowerCaseApiName = Senparc.CO2NET.Config.SenparcSetting.UseLowerCaseApiName ?? false;
            });

            //var apiGroups = ApiBindInfoCollection.Instance.GetGroupedCollection();
            //var apiGouupsCount = apiGroups.Count();
            //Console.WriteLine();
            //Console.WriteLine($"apiGouupsCount:{apiGouupsCount}");
            //Console.WriteLine();
            //Senparc.Weixin.AspNet.WeixinRegister.AddMcpRouter(services);


            return builder;
        }

        #endregion
    }
}
