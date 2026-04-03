using Microsoft.EntityFrameworkCore;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///AdminChatSessionModuleService: Manage background chat session-module associated service
    /// </summary>
    public class AdminChatSessionModuleService : BaseClientService<AdminChatSessionModule>
    {
        public AdminChatSessionModuleService(IAdminChatSessionModuleRepository repository, IServiceProvider serviceProvider) 
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        /// Get all modules associated with the session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        public async Task<List<AdminChatSessionModule>> GetSessionModulesAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId, "AddedTime ASC");
            return modules.ToList();
        }

        /// <summary>
        ///Add module to session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="xncfModuleUid">XNCF module UID</param>
        /// <param name="moduleName">Module name</param>
        /// <param name="moduleVersion">Module version</param>
        public async Task<AdminChatSessionModule> AddModuleToSessionAsync(int sessionId, string xncfModuleUid, string moduleName, string moduleVersion)
        {
            // Check if it already exists (prevent duplicate addition)
            var existing = await base.GetObjectAsync(m => 
                m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);

            if (existing != null)
            {
                return existing;
            }

            var module = new AdminChatSessionModule(sessionId, xncfModuleUid, moduleName, moduleVersion);
            await base.SaveObjectAsync(module);
            return module;
        }

        /// <summary>
        /// Batch add modules to session
        /// </summary>
        public async Task<List<AdminChatSessionModule>> AddModulesToSessionAsync(
            int sessionId, 
            List<(string uid, string name, string version)> modules)
        {
            var result = new List<AdminChatSessionModule>();

            foreach (var (uid, name, version) in modules)
            {
                var module = await AddModuleToSessionAsync(sessionId, uid, name, version);
                result.Add(module);
            }

            return result;
        }

        /// <summary>
        ///Remove module from session
        /// </summary>
        public async Task<bool> RemoveModuleFromSessionAsync(int sessionId, string xncfModuleUid)
        {
            var module = await base.GetObjectAsync(m => 
                m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);

            if (module == null) return false;

            await base.DeleteObjectAsync(module);
            return true;
        }

        /// <summary>
        ///Clear all module associations of the session
        /// </summary>
        public async Task<int> ClearSessionModulesAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId);

            foreach (var module in modules)
            {
                await base.DeleteObjectAsync(module);
            }

            return modules.Count();
        }

        /// <summary>
        /// Check if the module is associated to the session
        /// </summary>
        public async Task<bool> IsModuleInSessionAsync(int sessionId, string xncfModuleUid)
        {
            var module = await base.GetObjectAsync(m => m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);
            return module != null;
        }

        /// <summary>
        /// Get the number of modules in the session
        /// </summary>
        public async Task<int> GetSessionModuleCountAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId);
            return modules.Count();
        }

        /// <summary>
        /// Get the number of sessions used by the module (for statistics)
        /// </summary>
        public async Task<int> GetModuleUsageCountAsync(string xncfModuleUid)
        {
            var modules = await base.GetFullListAsync(m => m.XncfModuleUid == xncfModuleUid);
            return modules.Select(m => m.SessionId).Distinct().Count();
        }
    }
}
