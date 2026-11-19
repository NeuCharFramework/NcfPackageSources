/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentsTemplateService.cs
    File description: AgentsTemplateService related implementation

    Created by: Senparc - 20240616

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="agentTemplateDto"></param>
        /// <returns></returns>
        public async Task<AgentTemplateDto> UpdateAgentTemplateAsync(int id, AgentTemplateDto agentTemplateDto)
        {
            AgentTemplate agentTemplate = null;

            //Legacy comment translated to English.
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
