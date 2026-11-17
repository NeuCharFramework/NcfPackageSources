using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET;
using Senparc.Core.Models;
using Senparc.Ncf.Core;
using Senparc.Core;
using Senparc.Respository;

namespace Senparc.Web
{
    /// <summary>
    /// global registration
    /// </summary>
    public static class Register
    {
        public static void AddNcfServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env, CompatibilityVersion compatibilityVersion)
        {
            //Legacy comment translated to English.
            //https://docs.microsoft.com/zh-cn/aspnet/core/host-and-deploy/iis/index?view=aspnetcore-2.1&tabs=aspnetcore2x#supported-operating-systems
            //services.Configure<IISOptions>(options =>
            //{
            //    options.ForwardClientCertificate = false;
            //});


            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            //services.AddMvc(options =>
            //{
            //    //options.Filters.Add<HttpGlobalExceptionFilter>();
            //})
            //.SetCompatibilityVersion(compatibilityVersion)
            //.AddRazorPagesOptions(options =>
            //{
            //    //options.AllowAreas = true;//support Area
            //    //options.AllowMappingHeadRequestsToGetHandler = false;//https://www.learnrazorpages.com/razor-pages/handler-methods
            //})


            var builder = services.AddRazorPages()
              .AddNcfAreas()//Legacy comment translated to English.
              .AddXmlSerializerFormatters()
              .AddJsonOptions(options =>
              {
                  //Legacy comment translated to English.
                  //options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                  //Legacy comment translated to English.
                  //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                  //Legacy comment translated to English.
                  //options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
              })
              //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-2.1&tabs=aspnetcore2x
              //.AddSessionStateTempDataProvider()
              //Legacy comment translated to English.
              .AddRazorPagesOptions(options =>
              {
                  //Legacy comment translated to English.
                  options.Conventions.Add(new Core.Conventions.AutoValidateAntiForgeryTokenModelConvention());
              });
            ;


#if DEBUG
            //Legacy comment translated to English.
            if (env.IsDevelopment())
            {
                builder.AddRazorRuntimeCompilation(options =>
                {
                    var libraryPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "Senparc.Areas.Admin"));
                    options.FileProviders.Add(new PhysicalFileProvider(libraryPath));
                });
            }

#endif


            //Legacy comment translated to English.

            //support AutoMapper
            services.AddAutoMapper(_ => _.AddProfile<Core.AutoMapProfile.AutoMapperConfigs>());

            //support Session
            services.AddSession();
            //Legacy comment translated to English.
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
            //Legacy comment translated to English.
            services.AddMemoryCache();

            //Legacy comment translated to English.
            services.AddSignalR();
            //Legacy comment translated to English.
            services.AddTransient(typeof(Lazy<>));

            services.Configure<SenparcCoreSetting>(configuration.GetSection("SenparcCoreSetting"))
                .AddSenparcEntitiesDI(); //SQL Serversettings

            //Legacy comment translated to English.
            services.ScanAssamblesForAutoDI();
            //Legacy comment translated to English.
            AssembleScanHelper.RunScan();
            //services.AddSingleton<Core.Cache.RedisProvider.IRedisProvider, Core.Cache.RedisProvider.StackExchangeRedisProvider>();

            //Legacy comment translated to English.
            services.AddAuthorization(options =>
            {
                options.AddPolicy("UserAnonymous", policy =>
                {
                    policy.RequireClaim("UserMember");
                });
            });
            services.AddHttpContextAccessor();
            services.AddScoped(typeof(Areas.Admin.Filters.AuthenticationResultFilterAttribute));
            services.AddScoped(typeof(ISqlClientFinanceData), typeof(SqlClientFinanceData));
            services.AddScoped(typeof(ISqlBaseFinanceData), typeof(SqlClientFinanceData));
            services.AddScoped(typeof(ISysButtonRespository), typeof(SysButtonRespository));
            services.AddScoped(typeof(Core.WorkContext.Provider.IAdminWorkContextProvider), typeof(Core.WorkContext.Provider.AdminWorkContextProvider));
            services.AddTransient<Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor, Microsoft.AspNetCore.Mvc.Infrastructure.ActionContextAccessor>();
        }

    }
}
