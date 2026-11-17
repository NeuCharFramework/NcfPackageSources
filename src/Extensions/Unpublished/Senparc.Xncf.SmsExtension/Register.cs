/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.cs

    File description: Register related implementation

    

    

    Created by: Senparc - 20211124

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20261111

    Change description: v0.12.14 integrate SmsExtension Register display metadata with shared resource localization



----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.SMS;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.SmsExtension
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<SmsExtensionResource>(
            "Module.SmsExtension",
            "短信 插件",
            "短信 插件");

        #region Legacy section

        public override string Name => "Senparc.Xncf.SmsExtension";

        public override string Uid => "7917897C-796E-B02A-B329-5CA9007A9217";//Legacy comment translated to English.

        public override string Version => "0.1";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-file-word-o";

        public override string Description => RegisterText.Description;

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.Configure<SenparcSmsSetting>(configuration.GetSection("SenparcSmsSetting"));

            return base.AddXncfModule(services, configuration, env);
        }
    }
}
