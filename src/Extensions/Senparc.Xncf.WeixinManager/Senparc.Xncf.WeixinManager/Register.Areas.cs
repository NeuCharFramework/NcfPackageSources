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
    public partial class Register : IAreaRegister //注册 XNCF 页面接口（按需选用）
    {
        #region IAreaRegister 接口

        public string HomeUrl => "/Admin/WeixinManager/Index";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>() {
             new AreaPageMenuItem(GetAreaUrl("/Admin/WeixinManager/Index"),"首页","fa fa-laptop"),
             new AreaPageMenuItem(GetAreaUrl("/swagger"),"Web Api Swagger","fa fa-file-code-o"),
             new AreaPageMenuItem(GetAreaUrl("/Admin/WeixinManager/MpAccount"),"公众号管理","fa fa-comments"),
             new AreaPageMenuItem(GetAreaUrl("/Admin/WeixinManager/WeixinUser"),"用户管理","fa fa-users"),
        };

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {

            var services = builder.Services;
            //WebApiEngineExtensions.WebApiInitFinished = false;
            //启用 WebApi（可选）
            services.AddAndInitDynamicApi(builder, options =>
            {
                options.DefaultRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get;
                options.DocXmlPath = ServerUtility.ContentRootMapPath("~/App_Data/ApiDocXml");
                options.BaseApiControllerType = null;
                options.CopyCustomAttributes = true;
                options.TaskCount = Environment.ProcessorCount * 4;
                options.ShowDetailApiLog = true;
                options.AdditionalAttributeFunc = null;
                options.ForbiddenExternalAccess = false;
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
