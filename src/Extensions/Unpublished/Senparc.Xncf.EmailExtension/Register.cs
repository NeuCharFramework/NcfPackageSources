using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Senparc.Xncf.EmailExtension
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        /// <summary>
        /// 最大自动发送Email次数
        /// </summary>
        public static readonly int MaxSendEmailTimes = 5;

        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.EmailExtension";

        public override string Uid => "9439B96F-A6BE-0910-EE93-B46420ADC704";//必须确保全局唯一，生成后必须固定，已自动生成，也可自行修改

        public override string Version => "0.1";//必须填写版本号

        public override string MenuName => "Email 插件";

        public override string Icon => "fa fa-file-word-o";

        public override string Description => "Email 插件";

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            await unsinstallFunc().ConfigureAwait(false);
        }
        #endregion

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            return base.AddXncfModule(services, configuration, env);
        }
    }
}
