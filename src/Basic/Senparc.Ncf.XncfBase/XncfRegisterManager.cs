using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Senparc.Ncf.XncfBase
{
    public class XncfRegisterManager
    {
        #region 静态方法

        /// <summary>
        /// 模块和方法集合。
        /// </summary>
        public static List<IXncfRegister> RegisterList { get; set; } = new List<IXncfRegister>();

        /// <summary>
        /// 带有数据库的模块 TODO：可放置到缓存中
        /// </summary>
        public static List<IXncfDatabase> XncfDatabaseList => RegisterList.Where(z => z is IXncfDatabase).Select(z => z as IXncfDatabase).ToList();

        /// <summary>
        /// 判断指定名称的模块是否已注册
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsRegistered(string name) => RegisterList.Exists(z => z.Name == name);


        #endregion

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
            //检查内存中是否存在
            var xncfModuleRegister = IsRegistered(xncfName);
            if (!xncfModuleRegister)
            {
                return false;
            }
            //检查数据库中的安装状态
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
