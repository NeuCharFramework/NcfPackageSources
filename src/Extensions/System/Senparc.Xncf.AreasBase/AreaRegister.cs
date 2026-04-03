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
using System.Text.Json.Serialization;

namespace Senparc.Xncf.AreasBase
{
    /// <summary>
    ///Register all extension areas
    /// </summary>
    public static class AreaRegister
    {
        /// <summary>
        /// Automatically register all Areas
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="env"></param>
        /// <param name="eachRegsiterAction">Traverse to each Register additional operations</param>
        /// <returns></returns>
        public static IMvcBuilder AddNcfAreas(this IMvcBuilder builder, /*Microsoft.Extensions.Hosting.IHostEnvironment*/IWebHostEnvironment env, Action<IAreaRegister> eachRegsiterAction = null)
        {
            //Console.WriteLine("XncfRegisterManager:"+ XncfRegisterManager.RegisterList.Select(z=>z.Name).ToJson(true));
            var areaRegisters = XncfRegisterManager.RegisterList.Where(z => z is IAreaRegister).ToArray();

            foreach (var register in areaRegisters)
            {
                var areaRegister = register as IAreaRegister;

                Console.WriteLine("[IAreaRegister] " + register.Name);
                Console.WriteLine("[IAreaRegister] run AuthorizeConfig:" + areaRegister.AreaPageMenuItems.FirstOrDefault()?.Url);

                areaRegister.AuthorizeConfig(builder, env);//Register

                Console.WriteLine("[IAreaRegister] run AuthorizeConfig finished:" + areaRegister.AreaPageMenuItems.FirstOrDefault()?.Url);

                eachRegsiterAction?.Invoke(areaRegister);//perform additional actions
            }

            //No need to rescan anymore
            //AssembleScanHelper.AddAssembleScanItem(assembly =>
            //{
            //    try
            //    {
            //        //Filter
            //        if (assembly.FullName.StartsWith("Microsoft.Data.SqlClient"))
            //        {
            //            return;
            //        }

            //        var areaRegisterTypes = assembly.GetTypes()
            //                    .Where(z => z.GetInterface(nameof(IAreaRegister)) != null)
            //                    .ToArray();
            //        foreach (var registerType in areaRegisterTypes)
            //        {
            //            Console.WriteLine("[areaRegisterTypes] " + registerType.FullName);
            //            var register = Activator.CreateInstance(registerType, true) as IAreaRegister;
            //            if (register != null)
            //            {
            //                Console.WriteLine("[areaRegisterTypes] run AuthorizeConfig:" + register.AreaPageMenuItems.FirstOrDefault()?.Url);

            //                register.AuthorizeConfig(builder, env);//Register

            //                Console.WriteLine("[areaRegisterTypes] run AuthorizeConfig finished:" + register.AreaPageMenuItems.FirstOrDefault()?.Url);

            //                eachRegsiterAction?.Invoke(register);//Perform additional operations
            //            }
            //            else
            //            {
            //                SenparcTrace.BaseExceptionLog(new BaseException($"{registerType.Name} type does not implement the interface IAreaRegister!"));
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        var title = "AddNcfAreas() automatic scan assembly report (non-program exception):" + assembly.FullName;
            //        var message = ex.ToString();
            //        Console.WriteLine(title);
            //        Console.WriteLine(message);
            //        SenparcTrace.SendCustomLog(title, message);
            //    }
            //}, true);

            //All AuthorizeConfig methods have been executed
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllAuthorizeConfigApplied = true;


            return builder;
        }

        /// <summary>
        /// Start the NCF engine with Web function (if you do not need to use the Web, such as RazorPage, you can directly use <see cref="Senparc.Ncf.XncfBase.Register.StartEngine(IServiceCollection, IConfiguration)"/>)
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        /// <param name="addRazorPagesConfig">Internal delegate for services.AddRazorPages()</param>
        /// <param name="eachRegsiterAction">Traverse to each Register additional operations</param>
        /// <param name="dllFilePatterns">The file name of the included dll, ".Xncf." will definitely be included</param>
        /// <returns></returns>
        public static string StartWebEngine(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env,
            string[] dllFilePatterns,
            Action<RazorPagesOptions>? addRazorPagesConfig = null,
            Action<IAreaRegister> eachRegsiterAction = null)
        {
            var builder = services.AddRazorPages(addRazorPagesConfig)
             //Register all Ncf Area modules (required)
             .AddNcfAreas(env, eachRegsiterAction)
             //.AddJsonOptions(options =>
             //{
             //    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
             //})
             ;
            Console.WriteLine("临时：StartWebEngine");
            return services.StartNcfEngine(configuration, env, dllFilePatterns);
        }


        /// <summary>
        /// Start the NCF engine with Web function (if you do not need to use the Web, such as RazorPage, you can directly use <see cref="Senparc.Ncf.XncfBase.Register.StartEngine(IServiceCollection, IConfiguration)"/>)
        /// </summary>
        /// <typeparam name="TDatabaseConfiguration">Database type</typeparam>
        /// <param name="builder">WebApplicationBuilder</param>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        /// <param name="addRazorPagesConfig">Internal delegate for services.AddRazorPages()</param>
        /// <param name="eachRegsiterAction">Traverse to each Register additional operations</param>
        /// <param name="dllFilePatterns">The file name of the included dll, ".Xncf." will definitely be included</param>
        /// <returns></returns>
        public static string StartWebEngine(this WebApplicationBuilder builder,
                string[] dllFilePatterns,
                Action<RazorPagesOptions>? addRazorPagesConfig = null,
                Action<IAreaRegister> eachRegsiterAction = null)
        {
            var services = builder.Services;

            var startEngineLog = services.StartNcfEngine(builder.Configuration, builder.Environment, dllFilePatterns);

            //Add RazorPage and Area
            var mvcBuilder = services.AddRazorPages(addRazorPagesConfig)
                            //Register all Ncf Area modules (required)
                            .AddNcfAreas(builder.Environment, eachRegsiterAction);

            return startEngineLog;
        }
    }
}