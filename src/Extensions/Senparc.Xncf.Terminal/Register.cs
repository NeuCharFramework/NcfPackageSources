/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：Register 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.22.0-preview2 为 Terminal 模块接入统一资源本地化并优化功能文案

    修改标识：Senparc - 20260729
    修改描述：v0.22.1-preview3 收紧终端模块的命令执行说明和默认行为

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.Terminal
{
    [XncfRegister]
    public class Register : XncfRegisterBase, IXncfRegister
    {
        public Register()
        { }

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.Terminal";
        public override string Uid => "600C608A-F99A-4B1B-A18E-8CE69BE8DA92";//必须确保全局唯一，生成后必须固定
        public override string Version => "0.1.6";//必须填写版本号

        public override string MenuName => TerminalResource.Get("Module.Terminal.MenuName", "终端模块");
        public override string Icon => "fa fa-terminal";
        public override string Description => TerminalResource.Get("Module.Terminal.Description", "终端命令执行功能当前已禁用。");

        ///// <summary>
        ///// 注册当前模块需要支持的功能模块
        ///// </summary>
        //public override IList<Type> Functions => new[] { 
        //    typeof(Functions.Terminal),
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
