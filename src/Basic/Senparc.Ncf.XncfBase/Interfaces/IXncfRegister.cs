/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IXncfRegister.cs
    File description: IXncfRegister related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

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
    /// <summary>
    /// Defines the registration contract implemented by an NCF XNCF module.
    /// The contract supplies module metadata, lifecycle hooks, dependency-injection
    /// configuration, middleware, background-thread, and optional MCP integration points.
    /// </summary>
    public interface IXncfRegister
    {
        /// <summary>
        /// Gets a value indicating whether the module should be skipped by installation scanning.
        /// </summary>
        bool IgnoreInstall { get; }

        /// <summary>
        /// Gets a value indicating whether the module exposes an MCP server registration.
        /// </summary>
        bool EnableMcpServer { get; }

        /// <summary>
        /// Gets the stable assembly-qualified module name used for registration lookup.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the stable module identifier used to correlate registration and persisted records.
        /// </summary>
        string Uid { get; }

        /// <summary>
        /// Gets the semantic version exposed by this module registration.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the localized administration menu name.
        /// </summary>
        string MenuName { get; }

        /// <summary>
        /// Gets the icon identifier used by administration navigation.
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// Gets the localized module description shown in registration surfaces.
        /// </summary>
        string Description { get; }

        // Historical function-discovery contract retained for source compatibility reference.
        // IList<Type> Functions { get; }

        /// <summary>
        /// Gets or sets AutoMapper profile-registration actions retained until
        /// the application service container is finalized.
        /// </summary>
        ConcurrentBag<Action<Profile>> AutoMapMappingConfigs { get; set; }

        /// <summary>
        /// Gets the background thread registrations owned by this module.
        /// </summary>
        IEnumerable<KeyValuePair<ThreadInfo, Thread>> RegisteredThreadInfo { get; }

        /// <summary>
        /// Installs the module or applies a module upgrade.
        /// </summary>
        /// <param name="serviceProvider">The application service provider used to access module dependencies.</param>
        /// <param name="installOrUpdate">The lifecycle operation being performed.</param>
        /// <returns>A task that completes after installation or upgrade work finishes.</returns>
        Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate);

        /// <summary>
        /// Uninstalls the module and invokes the supplied finalization callback.
        /// </summary>
        /// <param name="serviceProvider">The application service provider used to access module dependencies.</param>
        /// <param name="unsinstallFunc">The callback that performs shared uninstall bookkeeping.</param>
        /// <returns>A task that completes after module cleanup finishes.</returns>
        Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc);

        /// <summary>
        /// Builds the root URL for the module's administration area.
        /// </summary>
        /// <returns>The area root URL.</returns>
        string GetAreaHomeUrl();

        /// <summary>
        /// Builds a URL beneath the module's administration area.
        /// </summary>
        /// <param name="path">The area-relative path to append.</param>
        /// <returns>The resulting module area URL.</returns>
        string GetAreaUrl(string path);

        /// <summary>
        /// Registers module services and configuration-dependent components.
        /// </summary>
        /// <param name="services">The application dependency-injection service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="env">The hosting environment.</param>
        /// <returns>The same service collection after module registration.</returns>
        IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env);

        /// <summary>
        /// Adds an AutoMapper profile-registration action to this module.
        /// </summary>
        /// <param name="mapping">The profile configuration action to retain.</param>
        void AddAutoMapMapping(Action<Profile> mapping);

        /// <summary>
        /// Applies module-specific AutoMapper registrations to the service collection.
        /// </summary>
        /// <param name="services">The application dependency-injection service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Adds the module middleware and returns the configured application pipeline.
        /// </summary>
        /// <param name="app">The ASP.NET Core application builder.</param>
        /// <param name="registerService">The shared NCF registration service.</param>
        /// <returns>The same application builder after module middleware is applied.</returns>
        IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService);

        #region MCP Server

        /// <summary>
        /// Registers the module's MCP server services.
        /// </summary>
        /// <param name="services">The application dependency-injection service collection.</param>
        /// <param name="xncfRegister">The registration whose MCP server is being configured.</param>
        void AddMcpServer(IServiceCollection services, IXncfRegister xncfRegister);
        /// <summary>
        /// Adds the module's MCP server endpoints or middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The ASP.NET Core application builder.</param>
        /// <param name="registerService">The shared NCF registration service.</param>
        void UseMcpServer(IApplicationBuilder app, IRegisterService registerService);

        #endregion
    }
}
