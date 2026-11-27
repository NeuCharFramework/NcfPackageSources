/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Register related implementation

    Created by: Senparc - 20230828

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.11.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.11.12 unify XNCF Register display metadata and localization resource keys

    Modified by: Senparc - 20261119
    Change description: v0.11.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr;

[XncfRegister]
public class Register : XncfRegisterBase, IXncfRegister
{
    private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<DaprResource>(
            "Module.Dapr",
            "Dapr 模块",
            "为其他模块提供 Dapr 相关能力");

    /// <summary>
    /// Initializes the Dapr XNCF registration metadata.
    /// </summary>
    public Register()
    { }

    #region Legacy section

    public override string Name => "Senparc.Xncf.Dapr";
    public override string Uid => "E2D87F61-BCA9-4F3E-9E5C-2A14B3F0C6D7";//Legacy comment translated to English.
    public override string Version => "0.11.14.2";//the version number is required

    public override string MenuName => RegisterText.MenuName;
    public override string Icon => "fa fa-car";
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
