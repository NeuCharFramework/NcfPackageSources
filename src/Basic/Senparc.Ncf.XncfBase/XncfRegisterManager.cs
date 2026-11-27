/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfRegisterManager.cs
    File description: XncfRegisterManager related implementation

    Created by: Senparc - 20200916

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

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
    /// Maintains discovered XNCF registrations and checks whether modules are installed and open.
    /// </summary>
    public class XncfRegisterManager
    {
        #region Legacy section

        /// <summary>
        /// Gets or sets the discovered XNCF module registrations.
        /// </summary>
        public static List<IXncfRegister> RegisterList { get; set; } = new List<IXncfRegister>();

        /// <summary>
        /// Gets the registered database-capable XNCF modules.
        /// </summary>
        public static List<IXncfDatabase> XncfDatabaseList => RegisterList.Where(z => z is IXncfDatabase).Select(z => z as IXncfDatabase).ToList();

        /// <summary>
        /// Gets the MCP server metadata indexed by server name.
        /// </summary>
        public static McpServerInfoCollection McpServerInfoCollection =new McpServerInfoCollection();

        /// <summary>
        /// Determines whether a registration with the supplied name is present.
        /// </summary>
        /// <param name="name">The registration name.</param>
        /// <returns><see langword="true"/> when a matching registration exists.</returns>
        public static bool IsRegistered(string name) => RegisterList.Exists(z => z.Name == name);

        /// <summary>
        /// Determines whether a registration with the same UID is present.
        /// </summary>
        /// <param name="xncfRegister">The registration to compare.</param>
        /// <returns><see langword="true"/> when a registration with the same UID exists.</returns>
        public static bool IsRegistered(IXncfRegister xncfRegister) => RegisterList.Exists(z => z.Uid == xncfRegister.Uid);


        #endregion

        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes the manager with the provider used to resolve module-state caches.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider.</param>
        public XncfRegisterManager(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider; 
        }

        /// <summary>
        /// Checks whether the persisted module record with the supplied name is open.
        /// </summary>
        /// <param name="xncfName">The registered module name.</param>
        /// <returns><see langword="true"/> when the persisted module exists and is open.</returns>
        private async Task<bool> CheckOpenStateAsync(string xncfName)
        {
            var fullXncfModuleCache = this._serviceProvider.GetService<FullXncfModuleCache>();
            var fullXncfModule = await fullXncfModuleCache.GetObjectAsync(xncfName);
            return fullXncfModule != null && fullXncfModule.State == Core.Enums.XncfModules_State.开放;
        }

        /// <summary>
        /// Determines whether a registered module is installed and currently open.
        /// </summary>
        /// <param name="xncfRegister">The module registration to inspect.</param>
        /// <returns><see langword="true"/> when the registration exists and its persisted state is open.</returns>
        public async Task<bool> CheckXncfAvailable(IXncfRegister xncfRegister)
        {
            return xncfRegister!=null && IsRegistered(xncfRegister)
                ? await CheckOpenStateAsync(xncfRegister.Name).ConfigureAwait(false)
                : false;
        }

        /// <summary>
        /// Determines whether a registered module name is installed and currently open.
        /// </summary>
        /// <param name="xncfName">The module registration name.</param>
        /// <returns><see langword="true"/> when the name is registered and open.</returns>
        public async Task<bool> CheckXncfAvailable(string xncfName)
        {
            return IsRegistered(xncfName)
                ? await CheckOpenStateAsync(xncfName).ConfigureAwait(false)
                : false;
        }
    }
}
