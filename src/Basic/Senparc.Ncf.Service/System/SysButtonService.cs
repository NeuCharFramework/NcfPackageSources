/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysButtonService.cs
    File description: SysButtonService related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Respository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service
{
    public class SysButtonService : ClientServiceBase<SysButton>
    {
        private readonly ISysButtonRespository _iSysButtonRespository;

        /// <summary>
        /// Initializes the button service with its specialized repository.
        /// </summary>
        /// <param name="repo">The repository used for button queries and deletion.</param>
        /// <param name="serviceProvider">The service provider used by the base service.</param>
        public SysButtonService(ISysButtonRespository repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
            _iSysButtonRespository = repo;
        }

        /// <summary>
        /// Deletes all buttons associated with a menu identifier.
        /// </summary>
        /// <param name="menuId">The menu identifier whose buttons should be deleted.</param>
        /// <returns>The number of deleted buttons, or zero for an empty menu identifier.</returns>
        public async Task<int> DeleteButtonsByMenuId(string menuId)
        {
            if (string.IsNullOrEmpty(menuId))
            {
                return 0;
            }
            return await _iSysButtonRespository.DeleteButtonsByMenuId(menuId);
        }

        /// <summary>
        /// Loads all buttons for a menu and maps them to DTOs.
        /// </summary>
        /// <param name="MenuId">The menu identifier used to filter button records.</param>
        /// <returns>The mapped button DTO sequence.</returns>
        public async Task<IEnumerable<SysButtonDto>> GetSysButtonDtosAsync(string MenuId)
        {
            IEnumerable<SysButton> sysButtons = await GetFullListAsync(_ => _.MenuId == MenuId);
            return Mapper.Map<IEnumerable<SysButtonDto>>(sysButtons);
        }
    }
}
