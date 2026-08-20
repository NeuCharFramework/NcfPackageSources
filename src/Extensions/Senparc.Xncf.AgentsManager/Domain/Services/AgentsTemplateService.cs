/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentsTemplateService.cs
    文件功能描述：AgentsTemplateService 相关实现
    
    
    创建标识：Senparc - 20240616
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class AgentsTemplateService : ServiceBase<AgentTemplate>
    {
        public AgentsTemplateService(IRepositoryBase<AgentTemplate> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        public async Task<AgentTemplate> GetAgentTemplateAsync(int id)
        {
            return await base.GetObjectAsync(x => x.Id == id);
        }

        /// <summary>
        /// 使用 AgentTemplateDto 进行更新
        /// </summary>
        /// <param name="id"></param>
        /// <param name="agentTemplateDto"></param>
        /// <returns></returns>
        public async Task<AgentTemplateDto> UpdateAgentTemplateAsync(int id, AgentTemplateDto agentTemplateDto)
        {
            AgentTemplate agentTemplate = null;

            //两者暂时等同
            agentTemplateDto.PromptCode = agentTemplateDto.SystemMessage;

            if (id > 0)
            {
                agentTemplate = await GetAgentTemplateAsync(id);
                agentTemplate.UpdateFromDto(agentTemplateDto);
            }

            if (agentTemplate == null)
            {
                agentTemplate = base.Mapper.Map<AgentTemplate>(agentTemplateDto);
            }

            //agentTemplate.EnableAgent();

            await base.SaveObjectAsync(agentTemplate);

            var newAgentTemplateDto = base.Mapping<AgentTemplateDto>(agentTemplate);
            return newAgentTemplateDto;
        }

    }
}
