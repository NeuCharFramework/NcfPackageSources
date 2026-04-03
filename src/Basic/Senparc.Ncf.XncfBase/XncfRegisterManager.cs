using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Senparc.Ncf.XncfBase.MCP;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    ///XncfRegister Manager
    /// </summary>
    public class XncfRegisterManager
    {
        #region 静态方法

        /// <summary>
        /// Collection of modules and methods. Modules have been sorted backwards according to Order (priority from high to low)
        /// </summary>
        public static List<IXncfRegister> RegisterList { get; set; } = new List<IXncfRegister>();

        /// <summary>
        /// Module with database TODO: can be placed in cache / TODO: can be removed
        /// </summary>
        public static List<IXncfDatabase> XncfDatabaseList => RegisterList.Where(z => z is IXncfDatabase).Select(z => z as IXncfDatabase).ToList();

        public static McpServerInfoCollection McpServerInfoCollection =new McpServerInfoCollection();

        /// <summary>
        /// Determine whether the module with the specified name has been registered
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsRegistered(string name) => RegisterList.Exists(z => z.Name == name);

        /// <summary>
        /// Determine whether the module with the specified name has been registered (recommended)
        /// </summary>
        /// <param name="xncfRegister">XncfRegister</param>
        /// <returns></returns>
        public static bool IsRegistered(IXncfRegister xncfRegister) => RegisterList.Exists(z => z.Uid == xncfRegister.Uid);


        #endregion

        private readonly IServiceProvider _serviceProvider;

        public XncfRegisterManager(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider; 
        }

        /// <summary>
        /// Check the module information in the database (cache) to see if it is open and in the open state
        /// </summary>
        /// <param name="xncfName"></param>
        /// <returns></returns>
        private async Task<bool> CheckOpenStateAsync(string xncfName)
        {
            //Check installation status in database
            var fullXncfModuleCache = this._serviceProvider.GetService<FullXncfModuleCache>();
            var fullXncfModule = await fullXncfModuleCache.GetObjectAsync(xncfName);
            return fullXncfModule != null && fullXncfModule.State == Core.Enums.XncfModules_State.开放;
        }

        /// <summary>
        /// Check if XNCF module is available (recommended)
        /// </summary>
        /// <param name="xncfRegister"></param>
        /// <returns></returns>
        public async Task<bool> CheckXncfAvailable(IXncfRegister xncfRegister)
        {
            //Check if it exists in memory
            return xncfRegister!=null && IsRegistered(xncfRegister)
                ? await CheckOpenStateAsync(xncfRegister.Name).ConfigureAwait(false)
                : false;
        }

        /// <summary>
        /// Check if XNCF module is available
        /// </summary>
        /// <param name="xncfName"></param>
        /// <returns></returns>
        public async Task<bool> CheckXncfAvailable(string xncfName)
        {
            //Check if it exists in memory
            return IsRegistered(xncfName)
                ? await CheckOpenStateAsync(xncfName).ConfigureAwait(false)
                : false;
        }
    }
}
