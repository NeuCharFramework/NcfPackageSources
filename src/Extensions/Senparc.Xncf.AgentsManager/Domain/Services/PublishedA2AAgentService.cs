/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PublishedA2AAgentService.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

----------------------------------------------------------------*/

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
