/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：Register 相关实现
    
    
    创建标识：Senparc - 20230828
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr;

[XncfRegister]
public class Register : XncfRegisterBase, IXncfRegister
{
    public Register()
    { }

    #region IXncfRegister 接口

    public override string Name => "Senparc.Xncf.Dapr";
    public override string Uid => "E2D87F61-BCA9-4F3E-9E5C-2A14B3F0C6D7";//必须确保全局唯一，生成后必须固定
    public override string Version => "0.0.1";//必须填写版本号

    public override string MenuName => "Dapr模块";
    public override string Icon => "fa fa-car";
    public override string Description => $"此模块为其他模块提供Dapr相关功能";

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
