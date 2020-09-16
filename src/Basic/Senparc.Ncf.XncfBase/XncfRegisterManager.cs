using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Ncf.XncfBase
{
    public class XncfRegisterManager
    {
        private readonly IServiceProvider _serviceProvider;

        public XncfRegisterManager(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 检查 XNCF 模块是否可用
        /// </summary>
        /// <param name="xncfName"></param>
        /// <returns></returns>
        public async Task<bool> CheckXncfValiable(string xncfName)
        {
            var xncfModuleRegister = Register.IsRegistered(xncfName);
            if (!xncfModuleRegister)
            {
                return false;
            }

            var fullXncfModuleCache = this._serviceProvider.GetService<FullXncfModuleCache>();
            var fullXncfModule = fullXncfModuleCache.GetObject(xncfName);
            if (fullXncfModule == null || fullXncfModule.State != Core.Enums.XncfModules_State.开放)
            {
                return false;
            }

            return true;
        }
    }
}
