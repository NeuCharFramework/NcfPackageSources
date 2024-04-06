using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Exceptions;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.AssembleScan;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using System;
using System.Linq;

namespace Senparc.Xncf.AreasBase
{
    /// <summary>
    /// 对所有扩展 Area 进行注册
    /// </summary>
    public static class AreaRegister
    {
        /// <summary>
        /// 自动注册所有 Area
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="env"></param>
        /// <param name="eachRegsiterAction">遍历到每一个 Register 额外的操作</param>
        /// <returns></returns>
        public static IMvcBuilder AddNcfAreas(this IMvcBuilder builder, /*Microsoft.Extensions.Hosting.IHostEnvironment*/IWebHostEnvironment env, Action<IAreaRegister> eachRegsiterAction = null)
        {
            AssembleScanHelper.AddAssembleScanItem(assembly =>
            {
                try
                {
                    var areaRegisterTypes = assembly.GetTypes()
                                .Where(z => z.GetInterface(nameof(IAreaRegister)) != null)
                                .ToArray();
                    foreach (var registerType in areaRegisterTypes)
                    {
                        Console.WriteLine("areaRegisterTypes:" + registerType.FullName);
                        var register = Activator.CreateInstance(registerType, true) as IAreaRegister;
                        if (register != null)
                        {
                            Console.WriteLine("areaRegisterTypes run AuthorizeConfig:" + register.AreaPageMenuItems.FirstOrDefault()?.Url);

                            register.AuthorizeConfig(builder, env);//进行注册

                            Console.WriteLine("areaRegisterTypes run AuthorizeConfig finished:" + register.AreaPageMenuItems.FirstOrDefault()?.Url);

                            eachRegsiterAction?.Invoke(register);//执行额外的操作
                        }
                        else
                        {
                            SenparcTrace.BaseExceptionLog(new BaseException($"{registerType.Name} 类型没有实现接口 IAreaRegister！"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    var title = "AddNcfAreas() 自动扫描程序集报告（非程序异常）：" + assembly.FullName;
                    var message = ex.ToString();
                    Console.WriteLine(title);
                    Console.WriteLine(message);
                    SenparcTrace.SendCustomLog(title, message);
                }
            }, true);

            //所有 AuthorizeConfig 方法已经执行完成
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllAuthorizeConfigApplied = true;


            return builder;
        }

        /// <summary>
        /// 启动带 Web 功能的 NCF 引擎（如不需要使用 Web，如 RazorPage，可以直接使用 <see cref="Senparc.Ncf.XncfBase.Register.StartEngine(IServiceCollection, IConfiguration)"/>）
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        /// <param name="addRazorPagesConfig">services.AddRazorPages() 的内部委托</param>
        /// <param name="eachRegsiterAction">遍历到每一个 Register 额外的操作</param>
        /// <param name="dllFilePatterns">被包含的 dll 的文件名，“.Xncf.”会被必定包含在里面</param>
        /// <returns></returns>
        public static string StartWebEngine(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env,
            string[] dllFilePatterns,
            Action<RazorPagesOptions>? addRazorPagesConfig = null,
            Action<IAreaRegister> eachRegsiterAction = null)
        {
            var builder = services.AddRazorPages(addRazorPagesConfig)
            //注册所有 Ncf 的 Area 模块（必须）
            .AddNcfAreas(env, eachRegsiterAction);
            Console.WriteLine("临时：StartWebEngine");
            return services.StartNcfEngine(configuration, env, dllFilePatterns);
        }

#if NET8_0_OR_GREATER

        /// <summary>
        /// 启动带 Web 功能的 NCF 引擎（如不需要使用 Web，如 RazorPage，可以直接使用 <see cref="Senparc.Ncf.XncfBase.Register.StartEngine(IServiceCollection, IConfiguration)"/>）
        /// </summary>
        /// <typeparam name="TDatabaseConfiguration">数据库类型</typeparam>
        /// <param name="builder">WebApplicationBuilder</param>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        /// <param name="addRazorPagesConfig">services.AddRazorPages() 的内部委托</param>
        /// <param name="eachRegsiterAction">遍历到每一个 Register 额外的操作</param>
        /// <param name="dllFilePatterns">被包含的 dll 的文件名，“.Xncf.”会被必定包含在里面</param>
        /// <returns></returns>
        public static string StartWebEngine(this WebApplicationBuilder builder,
                string[] dllFilePatterns,
                Action<RazorPagesOptions>? addRazorPagesConfig = null,
                Action<IAreaRegister> eachRegsiterAction = null)
        {
            var services = builder.Services;

            var startEngineLog = services.StartNcfEngine(builder.Configuration, builder.Environment, dllFilePatterns);

            //添加 RazorPage 和 Area
            var mvcBuilder = services.AddRazorPages(addRazorPagesConfig)
                            //注册所有 Ncf 的 Area 模块（必须）
                            .AddNcfAreas(builder.Environment, eachRegsiterAction);

            return startEngineLog;
        }
#endif
    }
}