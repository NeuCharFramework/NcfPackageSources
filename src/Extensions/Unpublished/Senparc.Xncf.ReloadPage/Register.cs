/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20211215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261111
    Change description: v0.12.14 integrate ReloadPage Register display metadata with shared resource localization

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.12.16.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Senparc.Xncf.ReloadPage.OHS.Hubs;
using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.RegisterServices;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.ReloadPage
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<ReloadPageResource>(
            "Module.ReloadPage",
            "自动重载页面模块",
            "自动重载页面模块");

        #region Legacy section

        public override string Name => "Senparc.Xncf.ReloadPage";

        public override string Uid => "F1415647-9AF8-9025-D21E-7943F553901C";//Legacy comment translated to English.

        public override string Version => "0.12.16.2";//the version number is required

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-file-word-o";

        public override string Description => RegisterText.Description;

        /// <inheritdoc />
        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
        }

        /// <inheritdoc />
        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        /// <inheritdoc />
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {

            return base.AddXncfModule(services, configuration, env);
        }

        /// <inheritdoc />
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            var hubContext = app.ApplicationServices.GetServices<IHubContext<ReloadPageHub>>();

            return base.UseXncfModule(app, registerService);
        }
    }
}
