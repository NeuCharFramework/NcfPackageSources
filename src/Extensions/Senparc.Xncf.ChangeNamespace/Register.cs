using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.ChangeNamespace.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.ChangeNamespace
{
    [XncfRegister]
    public class Register : XncfRegisterBase, IXncfRegister
    {
        public Register()
        { }

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.ChangeNamespace";
        public override string Uid => "476A8F12-860D-4B18-B703-393BBDEFBD85";//必须确保全局唯一，生成后必须固定
        public override string Version => "0.3.9";//必须填写版本号

        public override string MenuName => "修改命名空间";
        public override string Icon => "fa fa-space-shuttle";//参考如：https://colorlib.com/polygon/gentelella/icons.html
        public override string Description => "此模块提供给开发者在安装完 NCF、发布产品之前，全局修改命名空间，请在生产环境中谨慎使用，此操作不可逆！必须做好提前备份！不建议在已经部署至生产环境并开始运行后使用此功能！";

        /// <summary>
        /// 注册当前模块需要支持的功能模块
        /// </summary>
        public override IList<Type> Functions => new[] { 
            typeof(Functions.ChangeNamespace),
            typeof(Functions.RestoreNameSpace),
            typeof(Functions.DownloadSourceCode),
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
