/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: XncfRegisterManager.cs

    File description: XncfRegisterManager related implementation

    

    

    Created by: Senparc - 20200916

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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
    ///Legacy comment translated to English.
    /// </summary>
    public class XncfRegisterManager
    {
        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static List<IXncfRegister> RegisterList { get; set; } = new List<IXncfRegister>();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static List<IXncfDatabase> XncfDatabaseList => RegisterList.Where(z => z is IXncfDatabase).Select(z => z as IXncfDatabase).ToList();

        public static McpServerInfoCollection McpServerInfoCollection =new McpServerInfoCollection();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsRegistered(string name) => RegisterList.Exists(z => z.Name == name);

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="xncfName"></param>
        /// <returns></returns>
        private async Task<bool> CheckOpenStateAsync(string xncfName)
        {
            //Legacy comment translated to English.
            var fullXncfModuleCache = this._serviceProvider.GetService<FullXncfModuleCache>();
            var fullXncfModule = await fullXncfModuleCache.GetObjectAsync(xncfName);
            return fullXncfModule != null && fullXncfModule.State == Core.Enums.XncfModules_State.开放;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="xncfRegister"></param>
        /// <returns></returns>
        public async Task<bool> CheckXncfAvailable(IXncfRegister xncfRegister)
        {
            //Legacy comment translated to English.
            return xncfRegister!=null && IsRegistered(xncfRegister)
                ? await CheckOpenStateAsync(xncfRegister.Name).ConfigureAwait(false)
                : false;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="xncfName"></param>
        /// <returns></returns>
        public async Task<bool> CheckXncfAvailable(string xncfName)
        {
            //Legacy comment translated to English.
            return IsRegistered(xncfName)
                ? await CheckOpenStateAsync(xncfName).ConfigureAwait(false)
                : false;
        }
    }
}
