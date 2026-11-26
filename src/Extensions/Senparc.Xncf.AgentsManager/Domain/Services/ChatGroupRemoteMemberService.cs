/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupRemoteMemberService.cs
    File description: domain services and business-process implementation

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupRemoteMemberService : ServiceBase<ChatGroupRemoteMember>
    {
        /// <summary>
        /// Initializes the remote ChatGroup member persistence service.
        /// </summary>
        /// <param name="repo">The repository for remote-member entities.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public ChatGroupRemoteMemberService(IRepositoryBase<ChatGroupRemoteMember> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }
    }
}
