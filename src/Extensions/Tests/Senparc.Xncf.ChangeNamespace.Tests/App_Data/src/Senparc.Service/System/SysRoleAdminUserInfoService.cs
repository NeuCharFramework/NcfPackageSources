using AutoMapper;
using Senparc.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Service
{
    /// <summary>
    /// Replaces the role assignments associated with an administrator account.
    /// </summary>
    public class SysRoleAdminUserInfoService : ClientServiceBase<SysRoleAdminUserInfo>
    {
        private readonly SysRoleService _sysRoleService;

        /// <summary>
        /// Initializes the role-assignment service.
        /// </summary>
        /// <param name="repo">The role-assignment repository.</param>
        /// <param name="serviceProvider">The service provider used to resolve role data.</param>
        public SysRoleAdminUserInfoService(ClientRepositoryBase<SysRoleAdminUserInfo> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _sysRoleService = _serviceProvider.GetService<SysRoleService>();
        }

        /// <summary>
        /// Replaces all role links for an administrator account in one transaction.
        /// </summary>
        /// <param name="roleId">The role identifiers to assign.</param>
        /// <param name="accountId">The administrator account identifier.</param>
        /// <returns>A task that completes after replacement is persisted.</returns>
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

            await BeginTransactionAsync(async () => 
            {
                if (sysRoleAdmins.Any())
                {
                    await DeleteAllAsync(sysRoleAdmins);
                }
                if (sysRoleAdminUserInfos.Any())
                {
                    await SaveObjectListAsync(sysRoleAdminUserInfos);
                }
            }, ex => 
            {
                return new NotSupportedException("不支持");
            });
        }
    }
}
