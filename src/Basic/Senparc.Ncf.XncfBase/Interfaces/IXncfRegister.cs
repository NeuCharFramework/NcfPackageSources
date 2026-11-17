/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IXncfRegister.cs

    File description: IXncfRegister related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase
{
    public interface IXncfRegister
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        bool IgnoreInstall { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        bool EnableMcpServer { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string Uid { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// menu name
        /// </summary>
        string MenuName { get; }

        /// <summary>
        /// Iconicon
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// description
        /// </summary>
        string Description { get; }

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //IList<Type> Functions { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ConcurrentBag<Action<Profile>> AutoMapMappingConfigs { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc);

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        string GetAreaHomeUrl();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        string GetAreaUrl(string path);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="env"></param>
        /// <returns></returns>
        IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="mapping"></param>
        void AddAutoMapMapping(Action<Profile> mapping);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="app"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService);

        #region MCP Server

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="services"></param>
        void AddMcpServer(IServiceCollection services, IXncfRegister xncfRegister);
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService"></param>
        void UseMcpServer(IApplicationBuilder app, IRegisterService registerService);

        #endregion
    }
}
