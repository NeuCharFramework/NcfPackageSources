/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RemoteAgentService.cs
    File description: domain services and business-process implementation

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class RemoteAgentService : ServiceBase<RemoteAgent>
    {
        public RemoteAgentService(IRepositoryBase<RemoteAgent> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }
    }
}
