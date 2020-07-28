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
        public Register()
        { }

        #region IRegister 接口

        public override string Name => "Senparc.Xncf.Application";
        public override string Uid => "699DFE0D-C1C0-4315-87DF-0DE1502B87A9";//必须确保全局唯一，生成后必须固定
        public override string Version => "0.0.5";//必须填写版本号

        public override string MenuName => "应用程序模块";
        public override string Icon => "fa fa-pencil";
        public override string Description => "此模块提供给开发者一个可以启动任何程序！";

        /// <summary>
        /// 注册当前模块需要支持的功能模块
        /// </summary>
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
