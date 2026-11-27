/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AdminChatSessionModuleService.cs
    File description: AdminChatSessionModuleService related implementation

    Created by: Senparc - 20260325

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

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
    /// Provides persistence operations for the XNCF modules attached to administrator chat sessions.
    /// </summary>
    public class AdminChatSessionModuleService : BaseClientService<AdminChatSessionModule>
    {
        public AdminChatSessionModuleService(IAdminChatSessionModuleRepository repository, IServiceProvider serviceProvider) 
            : base(repository, serviceProvider)
        {
        }

        /// <summary>
        /// Returns the modules attached to a session in attachment order.
        /// </summary>
        /// <param name="sessionId">The chat session identifier to query.</param>
        /// <returns>A materialized list of session-module associations.</returns>
        public async Task<List<AdminChatSessionModule>> GetSessionModulesAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId, "AddedTime ASC");
            return modules.ToList();
        }

        /// <summary>
        /// Adds a module association when the same module is not already attached to the session.
        /// Existing associations are returned unchanged, making the operation idempotent.
        /// </summary>
        /// <param name="sessionId">The target chat session identifier.</param>
        /// <param name="xncfModuleUid">The stable XNCF module UID.</param>
        /// <param name="moduleName">The module name stored with the association.</param>
        /// <param name="moduleVersion">The module version stored with the association.</param>
        /// <returns>The existing or newly persisted session-module association.</returns>
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
        /// Adds each supplied module to a session through the idempotent single-module operation.
        /// </summary>
        /// <param name="sessionId">The target chat session identifier.</param>
        /// <param name="modules">The module UID, name, and version tuples to attach.</param>
        /// <returns>The existing or newly created associations in input order.</returns>
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
        /// Removes one module association from a session when it exists.
        /// </summary>
        /// <param name="sessionId">The chat session identifier.</param>
        /// <param name="xncfModuleUid">The XNCF module UID to remove.</param>
        /// <returns><see langword="true"/> when an association was deleted; otherwise <see langword="false"/>.</returns>
        public async Task<bool> RemoveModuleFromSessionAsync(int sessionId, string xncfModuleUid)
        {
            var module = await base.GetObjectAsync(m => 
                m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);

            if (module == null) return false;

            await base.DeleteObjectAsync(module);
            return true;
        }

        /// <summary>
        /// Deletes every module association belonging to a session.
        /// </summary>
        /// <param name="sessionId">The chat session identifier to clear.</param>
        /// <returns>The number of associations found and deleted.</returns>
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
        /// Determines whether a specific module is attached to a session.
        /// </summary>
        /// <param name="sessionId">The chat session identifier.</param>
        /// <param name="xncfModuleUid">The XNCF module UID to find.</param>
        /// <returns><see langword="true"/> when the association exists.</returns>
        public async Task<bool> IsModuleInSessionAsync(int sessionId, string xncfModuleUid)
        {
            var module = await base.GetObjectAsync(m => m.SessionId == sessionId && m.XncfModuleUid == xncfModuleUid);
            return module != null;
        }

        /// <summary>
        /// Counts the module associations attached to a session.
        /// </summary>
        /// <param name="sessionId">The chat session identifier.</param>
        /// <returns>The number of attached module associations.</returns>
        public async Task<int> GetSessionModuleCountAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(m => m.SessionId == sessionId);
            return modules.Count();
        }

        /// <summary>
        /// Counts distinct chat sessions that use a module UID.
        /// </summary>
        /// <param name="xncfModuleUid">The XNCF module UID to count.</param>
        /// <returns>The number of distinct sessions containing the module.</returns>
        public async Task<int> GetModuleUsageCountAsync(string xncfModuleUid)
        {
            var modules = await base.GetFullListAsync(m => m.XncfModuleUid == xncfModuleUid);
            return modules.Select(m => m.SessionId).Distinct().Count();
        }
    }
}
