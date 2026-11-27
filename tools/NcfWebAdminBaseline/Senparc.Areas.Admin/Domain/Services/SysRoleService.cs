/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SysRoleService.cs
    File description: Senparc.Areas.Admin.Domain.Services related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class SysRoleService : BaseClientService<SysRole>
    {
        /// <summary>
        /// Initializes the role service with its repository and request service provider.
        /// </summary>
        /// <param name="repo">The repository used for role persistence.</param>
        /// <param name="serviceProvider">The request service provider.</param>
        public SysRoleService(ClientRepositoryBase<SysRole> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
    }
}
