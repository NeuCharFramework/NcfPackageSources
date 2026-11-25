/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRoleService.cs
    File description: SysRoleService related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service
{
    public class SysRoleService : ClientServiceBase<SysRole>
    {
        /// <summary>
        /// Initializes the role service with its repository and dependency provider.
        /// </summary>
        /// <param name="repo">The repository for role persistence.</param>
        /// <param name="serviceProvider">The service provider used by the base service.</param>
        public SysRoleService(ClientRepositoryBase<SysRole> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {

        }

        /// <summary>
        /// Creates a new role or updates the existing role identified by the DTO.
        /// </summary>
        /// <param name="dto">The role values to create or persist.</param>
        /// <returns>A task that completes after the role has been saved.</returns>
        public async Task CreateOrUpdateAsync(SysRoleDto dto)
        {
            SysRole sysRole;
            if (String.IsNullOrEmpty(dto.Id))
            {
                sysRole = new SysRole(dto);
            }
            else
            {
                sysRole = await GetObjectAsync(_ => _.Id == dto.Id);
                sysRole.Update(dto);
            }
            await SaveObjectAsync(sysRole);
        }
    }
}
