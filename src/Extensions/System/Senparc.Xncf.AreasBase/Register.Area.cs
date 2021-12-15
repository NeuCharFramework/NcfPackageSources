using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using System.Collections.Generic;

namespace Senparc.Xncf.AreasBase
{
    public partial class Register : IAreaRegister
    {
        public string HomeUrl => "";

        public List<AreaPageMenuItem> AareaPageMenuItems => new List<AreaPageMenuItem>();

        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            builder.Services.AddRazorPages(opt =>
            {
                //opt.RootDirectory = "/";
            })
              .AddNcfAreas(env)//注册所有 Ncf 的 Area 模块（必须）  TODO：需要在外部引用，这个是入口！！！
              .AddXmlSerializerFormatters()
              .AddJsonOptions(options =>
              {
                  //忽略循环引用
                  //options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                  //不使用驼峰样式的key
                  //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                  //设置时间格式
                  //options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
              })
              //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-2.1&tabs=aspnetcore2x
              //.AddSessionStateTempDataProvider()
              //忽略JSON序列化过程中的循环引用：https://stackoverflow.com/questions/7397207/json-net-error-self-referencing-loop-detected-for-type
              .AddRazorPagesOptions(options =>
              {
                  //自动注册  防止跨站请求伪造（XSRF/CSRF）攻击
                  options.Conventions.Add(new Senparc.Ncf.AreaBase.Conventions.AutoValidateAntiForgeryTokenModelConvention());
              });

            //提供网站根目录
            if (env.ContentRootPath != null)
            {
                SiteConfig.ApplicationPath = env.ContentRootPath;
                if (env is IWebHostEnvironment webEnv)
                {
                    SiteConfig.WebRootPath = webEnv.WebRootPath;
                }
            }

            return builder;
        }
    }
}
