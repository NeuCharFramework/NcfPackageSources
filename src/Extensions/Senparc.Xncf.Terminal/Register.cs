/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.22.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260729
    Change description: v0.22.1-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.22.12 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.22.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.22.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.Terminal
{
    [XncfRegister]
    /// <summary>Registers the terminal-command XNCF module.</summary>
    public class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<TerminalResource>(
            "Module.Terminal",
            "终端模块",
            "允许开发者直接使用终端命令控制系统。");

        /// <summary>
        /// Initializes the Terminal XNCF registration metadata.
        /// </summary>
        public Register()
        { }

        #region Module metadata

        /// <inheritdoc />
        public override string Name => "Senparc.Xncf.Terminal";
        /// <inheritdoc />
        public override string Uid => "600C608A-F99A-4B1B-A18E-8CE69BE8DA92";
        /// <inheritdoc />
        public override string Version => "0.22.14.2";//the version number is required

        /// <inheritdoc />
        public override string MenuName => RegisterText.MenuName;
        /// <inheritdoc />
        public override string Icon => "fa fa-terminal";
        /// <inheritdoc />
        public override string Description => RegisterText.Description;

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
