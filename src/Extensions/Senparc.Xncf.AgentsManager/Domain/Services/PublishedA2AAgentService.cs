using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class PublishedA2AAgentService : ServiceBase<PublishedA2AAgent>
    {
        public PublishedA2AAgentService(IRepositoryBase<PublishedA2AAgent> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public Task<PublishedA2AAgent> GetByAgentTemplateIdAsync(int agentTemplateId)
        {
            return GetObjectAsync(z => z.AgentTemplateId == agentTemplateId);
        }

        public Task<PublishedA2AAgent> GetByPublicAgentKeyAsync(string publicAgentKey)
        {
            return GetObjectAsync(z => z.PublicAgentKey == publicAgentKey);
        }
    }
}
