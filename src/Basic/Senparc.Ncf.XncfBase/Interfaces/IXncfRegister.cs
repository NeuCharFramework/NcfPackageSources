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
        /// Whether to ignore installation (but does not affect the execution of registration code)
        /// </summary>
        bool IgnoreInstall { get; }

        /// <summary>
        /// Whether to enable MCP server (MCP Server)
        /// </summary>
        bool EnableMcpServer { get; }

        /// <summary>
        ///Module name, required to be globally unique
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Number, required to be globally unique
        /// </summary>
        string Uid { get; }

        /// <summary>
        /// version number
        /// </summary>
        string Version { get; }

        /// <summary>
        ///menu name
        /// </summary>
        string MenuName { get; }

        /// <summary>
        ///icon
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// illustrate
        /// </summary>
        string Description { get; }

        ///// <summary>
        ///// Registration method, the order of registration determines the order of arrangement in the interface
        ///// </summary>
        //IList<Type> Functions { get; }

        /// <summary>
        ///Add AutoMap mapping
        /// </summary>
        ConcurrentBag<Action<Profile>> AutoMapMappingConfigs { get; set; }

        /// <summary>
        /// Get the registered thread information of the current module
        /// </summary>
        IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo { get; }

        /// <summary>
        ///Installation code
        /// </summary>
        Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate);

        /// <summary>
        ///uninstall code
        /// </summary>
        Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc);

        /// <summary>
        /// Get homepage Url
        /// <para>Only Register after implementing the IAreaRegister interface, otherwise null will be returned</para>
        /// </summary>
        /// <returns></returns>
        string GetAreaHomeUrl();

        /// <summary>
        /// Get the URLs of other pages in Area
        /// </summary>
        /// <param name="path">URL path (without uid parameter)</param>
        /// <returns></returns>
        string GetAreaUrl(string path);

        /// <summary>
        /// Register the current module when ConfigureServices starts
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="env"></param>
        /// <returns></returns>
        IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env);

        /// <summary>
        /// Add AutoMap mapping relationship
        /// </summary>
        /// <param name="mapping"></param>
        void AddAutoMapMapping(Action<Profile> mapping);

        /// <summary>
        ///Execute AutoMapper mapping
        /// </summary>
        void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Perform configuration in the Configure() method of startup.cs
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService">CO2NET registration object</param>
        /// <returns></returns>
        IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService);

        #region MCP Server

        /// <summary>
        ///Add MCP server
        /// </summary>
        /// <param name="services"></param>
        void AddMcpServer(IServiceCollection services, IXncfRegister xncfRegister);
        /// <summary>
        /// use MCP server
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService"></param>
        void UseMcpServer(IApplicationBuilder app, IRegisterService registerService);

        #endregion
    }
}
