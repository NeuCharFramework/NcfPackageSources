/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20211124

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261111
    Change description: v0.12.14 integrate EmailExtension Register display metadata with shared resource localization

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
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.EmailExtension
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<EmailExtensionResource>(
            "Module.EmailExtension",
            "Email 插件",
            "Email 插件");

        /// <summary>
        /// Gets the maximum number of attempts made before a queued email is abandoned.
        /// </summary>
        public static readonly int MaxSendEmailTimes = 5;

        #region Legacy section

        public override string Name => "Senparc.Xncf.EmailExtension";

        public override string Uid => "9439B96F-A6BE-0910-EE93-B46420ADC704";

        public override string Version => "0.12.16.2";//the version number is required

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

        /// <inheritdoc />
        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            return base.AddXncfModule(services, configuration, env);
        }
    }
}
