using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.AspNet;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.SMS;
using Senparc.Ncf.XncfBase;
using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Senparc.IntegrationSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //提供网站根目录
            if (Env.ContentRootPath != null)
            {
                SiteConfig.ApplicationPath = Env.ContentRootPath;
                SiteConfig.WebRootPath = Env.WebRootPath;
            }

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSenparcGlobalServices(Configuration);

            services.AddRazorPages();

            //支持 Session
            services.AddSession();
            //解决中文进行编码问题
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
            //使用内存缓存
            services.AddMemoryCache();

            //注册 Lazy<T>
            services.AddTransient(typeof(Lazy<>));

            services.Configure<SenparcCoreSetting>(Configuration.GetSection("SenparcCoreSetting"));
            services.Configure<SenparcSmsSetting>(Configuration.GetSection("SenparcSmsSetting"));

            //自动依赖注入扫描
            services.ScanAssamblesForAutoDI();
            //已经添加完所有程序集自动扫描的委托，立即执行扫描（必须）
            AssembleScanHelper.RunScan();
            services.AddHttpContextAccessor();
            //激活 Xncf 扩展引擎（必须）
            services.StartEngine(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IOptions<SenparcCoreSetting> senparcCoreSetting,
            IOptions<SenparcSetting> senparcSetting)
        {
            var registerService = app
                //全局注册
                .UseSenparcGlobal(env, senparcSetting.Value, globalRegister =>
                 {
                 });

            //XncfModules（必须）
            Senparc.Ncf.XncfBase.Register.UseXncfModules(app, registerService);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
