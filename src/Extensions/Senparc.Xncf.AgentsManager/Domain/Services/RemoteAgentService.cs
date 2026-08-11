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
