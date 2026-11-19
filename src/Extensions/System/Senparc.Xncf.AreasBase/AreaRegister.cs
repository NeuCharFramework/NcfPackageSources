/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AreaRegister.cs
    File description: AreaRegister related implementation

    Created by: Senparc - 20211215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.13.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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
    ///Legacy comment translated to English.
    /// </summary>
    public static class AreaRegister
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="env"></param>
        ///Legacy comment translated to English.
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

                areaRegister.AuthorizeConfig(builder, env);//Legacy comment translated to English.

                Console.WriteLine("[IAreaRegister] run AuthorizeConfig finished:" + areaRegister.AreaPageMenuItems.FirstOrDefault()?.Url);

                eachRegsiterAction?.Invoke(areaRegister);//Legacy comment translated to English.
            }

            //Legacy comment translated to English.
            //AssembleScanHelper.AddAssembleScanItem(assembly =>
            //{
            //    try
            //    {
            //Legacy comment translated to English.
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

            //Legacy comment translated to English.

            //                Console.WriteLine("[areaRegisterTypes] run AuthorizeConfig finished:" + register.AreaPageMenuItems.FirstOrDefault()?.Url);

            //Legacy comment translated to English.
            //            }
            //            else
            //            {
            //Legacy comment translated to English.
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //Legacy comment translated to English.
            //        var message = ex.ToString();
            //        Console.WriteLine(title);
            //        Console.WriteLine(message);
            //        SenparcTrace.SendCustomLog(title, message);
            //    }
            //}, true);

            //all AuthorizeConfig methodalreadyexecutecomplete
            Ncf.Core.Config.SiteConfig.NcfCoreState.AllAuthorizeConfigApplied = true;


            return builder;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <param name="dllFilePatterns">File name of the included dll, ".Xncf."will definitely be included inside</param>
        /// <returns></returns>
        public static string StartWebEngine(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env,
            string[] dllFilePatterns,
            Action<RazorPagesOptions>? addRazorPagesConfig = null,
            Action<IAreaRegister> eachRegsiterAction = null)
        {
            var builder = services.AddRazorPages(addRazorPagesConfig)
             //Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="TDatabaseConfiguration">databasetype</typeparam>
        /// <param name="builder">WebApplicationBuilder</param>
        /// <param name="configuration"></param>
        /// <param name="env"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <param name="dllFilePatterns">File name of the included dll, ".Xncf."will definitely be included inside</param>
        /// <returns></returns>
        public static string StartWebEngine(this WebApplicationBuilder builder,
                string[] dllFilePatterns,
                Action<RazorPagesOptions>? addRazorPagesConfig = null,
                Action<IAreaRegister> eachRegsiterAction = null)
        {
            var services = builder.Services;

            var startEngineLog = services.StartNcfEngine(builder.Configuration, builder.Environment, dllFilePatterns);

            //Legacy comment translated to English.
            var mvcBuilder = services.AddRazorPages(addRazorPagesConfig)
                            //Legacy comment translated to English.
                            .AddNcfAreas(builder.Environment, eachRegsiterAction);

            return startEngineLog;
        }
    }
}