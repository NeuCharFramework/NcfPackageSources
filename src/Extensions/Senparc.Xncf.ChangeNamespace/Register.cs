/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.22.0-preview2 integrate shared resource localization and improve feature text for the ChangeNamespace module

    Modified by: Senparc - 20261111
    Change description: v0.22.12 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.ChangeNamespace
{
    [XncfRegister]
    public class Register : XncfRegisterBase, IXncfRegister
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<ChangeNamespaceResource>(
            "Module.ChangeNamespace",
            "修改命名空间",
            "在 NCF 安装后、产品发布前全局修改源码命名空间。此操作不可逆，请提前备份，且不要在已运行的生产环境中使用。");

        public Register()
        { }

        #region Legacy section

        public override string Name => "Senparc.Xncf.ChangeNamespace";
        public override string Uid => "476A8F12-860D-4B18-B703-393BBDEFBD85";//Legacy comment translated to English.
        public override string Version => "0.3.9";//the version number is required

        public override string MenuName => RegisterText.MenuName;
        public override string Icon => "fa fa-space-shuttle";//Legacy comment translated to English.
        public override string Description => RegisterText.Description;

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //public override IList<Type> Functions => new[] { 
        //    typeof(Functions.ChangeNamespace),
        //    typeof(Functions.RestoreNameSpace),
        //    typeof(Functions.DownloadSourceCode),
        //};

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
