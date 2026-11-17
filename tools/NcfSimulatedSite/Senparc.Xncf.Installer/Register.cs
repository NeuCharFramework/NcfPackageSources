/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.cs

    File description: module registration and initialization logic

    

    

    Created by: Senparc - 20241028

    

    Modified by: Senparc - 20260724

    Change description: v0.4.0 Updated module behavior and related support.



    Modified by: Senparc - 20261111

    Change description: v0.5.14 unify XNCF Register display metadata and localization resource keys



----------------------------------------------------------------*/
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.Installer.Domain.Services;
using Senparc.Xncf.Installer.OHS.Local.AppService;
using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace Senparc.Xncf.Installer
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister, IAreaRegister/*, IXncfDatabase*/
    {
        private static readonly XncfRegisterText RegisterText =
            XncfRegisterText.Create<InstallerResource>(
                "Installer.Register",
                "NCF 安装程序",
                "NCF 初始化安装模块，安装完成后可卸载或移除");

        #region Legacy section

        public override string Name => "Senparc.Xncf.Installer";

        public override string Uid => "62FBB022-B04E-423F-82FE-926D418A0815";//Legacy comment translated to English.

        public override string Version => "0.3";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-download";

        public override string Description => RegisterText.Description;

        public string DatabaseUniquePrefix => throw new NotImplementedException();

        public Type TryGetXncfDatabaseDbContextType => throw new NotImplementedException();

        public override Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddScoped<InstallerService>();
            services.AddScoped<InstallAppService>();
            services.AddScoped<InstallOptionsService>();
            return base.AddXncfModule(services, configuration, env);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });

            return base.UseXncfModule(app, registerService);
        }

        //#region Database

        //public void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}

        //public void AddXncfDatabaseModule(IServiceCollection services)
        //{
        //}

        //#endregion
    }
}
