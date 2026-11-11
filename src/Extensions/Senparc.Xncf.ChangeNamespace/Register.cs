/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.cs
    文件功能描述：Register 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.22.0-preview2 为 ChangeNamespace 模块接入统一资源本地化并优化功能文案

    修改标识：Senparc - 20261111
    修改描述：v0.22.12 统一 XNCF Register 显示元数据与多语言资源键

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

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.ChangeNamespace";
        public override string Uid => "476A8F12-860D-4B18-B703-393BBDEFBD85";//必须确保全局唯一，生成后必须固定
        public override string Version => "0.3.9";//必须填写版本号

        public override string MenuName => RegisterText.MenuName;
        public override string Icon => "fa fa-space-shuttle";//参考如：https://colorlib.com/polygon/gentelella/icons.html
        public override string Description => RegisterText.Description;

        ///// <summary>
        ///// 注册当前模块需要支持的功能模块
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
