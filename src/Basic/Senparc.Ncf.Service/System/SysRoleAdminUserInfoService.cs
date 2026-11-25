/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRoleAdminUserInfoService.cs
    File description: SysRoleAdminUserInfoService related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models.DataBaseModel;

namespace Senparc.Ncf.Service
{
    /// <summary>
    /// Manages the association between administrator accounts and system roles.
    /// </summary>
    public class SysRoleAdminUserInfoService : ClientServiceBase<SysRoleAdminUserInfo>
    {
        private readonly SysRoleService _sysRoleService;

        /// <summary>
        /// Initializes the administrator-role association service.
        /// </summary>
        /// <param name="repo">The repository used to persist role associations.</param>
        /// <param name="serviceProvider">The provider used to resolve the role service.</param>
        public SysRoleAdminUserInfoService(ClientRepositoryBase<SysRoleAdminUserInfo> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _sysRoleService = _serviceProvider.GetService<SysRoleService>();
        }

        /// <summary>
        /// Replaces all role associations for an administrator account with the supplied role identifiers.
        /// The deletion and insertion are executed through a resilient transaction.
        /// </summary>
        /// <param name="roleId">The role identifiers to associate with the account.</param>
        /// <param name="accountId">The administrator account identifier.</param>
        /// <returns>A task that completes after the replacement associations are persisted.</returns>
        public async Task AddAsync(IEnumerable<string> roleId, int accountId)
        {
            IList<SysRoleAdminUserInfo> sysRoleAdminUserInfos = new List<SysRoleAdminUserInfo>();

            IEnumerable<SysRole> sysRoles = await _sysRoleService.GetFullListAsync(_ => roleId.Contains(_.Id));
            foreach (var item in roleId)
            {
                SysRoleAdminUserInfo sysRoleAdminUserInfo = new SysRoleAdminUserInfo(accountId, item, sysRoles.FirstOrDefault(_ => _.Id == item)?.RoleCode);
                sysRoleAdminUserInfos.Add(sysRoleAdminUserInfo);
            }

            IEnumerable<SysRoleAdminUserInfo> sysRoleAdmins = await GetFullListAsync(_ => _.AccountId == accountId);
            await ServiceBase.ResilientTransaction.New(BaseData.BaseDB.BaseDataContext).ExecuteAsync(async () =>
            {
                if (sysRoleAdmins.Any())
                {
                    await DeleteAllAsync(sysRoleAdmins);
                }
                if (sysRoleAdminUserInfos.Any())
                {
                    await SaveObjectListAsync(sysRoleAdminUserInfos);
                }
            });
        }
    }
}
