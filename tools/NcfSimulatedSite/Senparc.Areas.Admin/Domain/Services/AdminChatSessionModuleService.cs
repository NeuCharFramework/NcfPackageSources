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
    ///Legacy comment translated to English.
    /// </summary>
    public class AdminChatSessionModuleService : BaseClientService<AdminChatSessionModule>
    {
        public AdminChatSessionModuleService(IAdminChatSessionModuleRepository repository, IServiceProvider serviceProvider) 
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        public async Task<List<AdminChatSessionModule>> GetSessionModulesAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId, "AddedTime ASC");
            return modules.ToList();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public async Task<AdminChatSessionModule> AddModuleToSessionAsync(int sessionId, string xncfModuleUid, string moduleName, string moduleVersion)
        {
            //Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<bool> IsModuleInSessionAsync(int sessionId, string xncfModuleUid)
        {
            var module = await base.GetObjectAsync(m => m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);
            return module != null;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<int> GetSessionModuleCountAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId);
            return modules.Count();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public async Task<int> GetModuleUsageCountAsync(string xncfModuleUid)
        {
            var modules = await base.GetFullListAsync(m => m.XncfModuleUid == xncfModuleUid);
            return modules.Select(m => m.SessionId).Distinct().Count();
        }
    }
}
