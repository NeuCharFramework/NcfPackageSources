/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.1.0 integrate shared resource localization and improve feature text for the Application module

    Modified by: Senparc - 20261111
    Change description: v0.1.12 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.1.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.Application.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.Application
{
    [XncfRegister]
    public class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<ApplicationResource>(
            "Module.Application",
            "应用程序模块",
            "允许开发者启动外部程序");

        /// <summary>
        /// Initializes the Application XNCF registration metadata.
        /// </summary>
        public Register()
        { }

        #region Legacy section

        public override string Name => "Senparc.Xncf.Application";
        public override string Uid => "699DFE0D-C1C0-4315-87DF-0DE1502B87A9";//Legacy comment translated to English.
        public override string Version => "0.0.5";//the version number is required

        public override string MenuName => RegisterText.MenuName;
        public override string Icon => "fa fa-pencil";
        public override string Description => RegisterText.Description;

        /// <inheritdoc />
        public override IList<Type> Functions => new[] {
            typeof(Functions.LaunchApp),
        };

        public override Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            return Task.CompletedTask;
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }

        #endregion
    }
}
