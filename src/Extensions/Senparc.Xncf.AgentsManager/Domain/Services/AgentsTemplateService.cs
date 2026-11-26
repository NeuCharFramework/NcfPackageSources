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
    /// <summary>Provides persistence operations for Agent templates.</summary>
    public class AgentsTemplateService : ServiceBase<AgentTemplate>
    {
        /// <summary>Initializes the Agent-template service.</summary>
        /// <param name="repo">The Agent-template repository.</param>
        /// <param name="serviceProvider">The dependency-injection service provider.</param>
        public AgentsTemplateService(IRepositoryBase<AgentTemplate> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>Gets an Agent template by persisted identifier.</summary>
        /// <param name="id">The Agent-template identifier.</param>
        /// <returns>The matching template, or <see langword="null"/>.</returns>
        public async Task<AgentTemplate> GetAgentTemplateAsync(int id)
        {
            return await base.GetObjectAsync(x => x.Id == id);
        }

        /// <summary>
        /// Updates an existing Agent template or creates a new one from the DTO.
        /// </summary>
        /// <param name="id">The existing template identifier, or a non-positive value to create.</param>
        /// <param name="agentTemplateDto">The template values to persist.</param>
        /// <returns>The mapped persisted template DTO.</returns>
        public async Task<AgentTemplateDto> UpdateAgentTemplateAsync(int id, AgentTemplateDto agentTemplateDto)
        {
            AgentTemplate agentTemplate = null;

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
