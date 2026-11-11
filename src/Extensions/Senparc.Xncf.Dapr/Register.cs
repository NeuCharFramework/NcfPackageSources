/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：Register 相关实现
    
    
    创建标识：Senparc - 20230828
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.11.0-preview2 为 Dapr 模块接入统一资源本地化并优化功能文案

    修改标识：Senparc - 20261111
    修改描述：v0.11.12 统一 XNCF Register 显示元数据与多语言资源键

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

    public Register()
    { }

    #region IXncfRegister 接口

    public override string Name => "Senparc.Xncf.Dapr";
    public override string Uid => "E2D87F61-BCA9-4F3E-9E5C-2A14B3F0C6D7";//必须确保全局唯一，生成后必须固定
    public override string Version => "0.0.1";//必须填写版本号

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
