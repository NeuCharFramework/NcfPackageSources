/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PublishedA2AAgentAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20260813

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.Application.Dtos;
using Senparc.Xncf.AgentsManager.OHS.Remote.Controllers;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// Provides authenticated APIs for publishing local Agent templates through A2A.
    /// </summary>
    [ApiAuthorize]
    public class PublishedA2AAgentAppService : AppServiceBase
    {
        private readonly PublishedA2AAgentService _publishedA2AAgentService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes published A2A-agent management services.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base application service.</param>
        /// <param name="publishedA2AAgentService">The published-agent persistence service.</param>
        /// <param name="agentsTemplateService">The local AgentTemplate service.</param>
        /// <param name="httpContextAccessor">Accessor used to construct the public agent-card URL.</param>
        public PublishedA2AAgentAppService(
            IServiceProvider serviceProvider,
            PublishedA2AAgentService publishedA2AAgentService,
            AgentsTemplateService agentsTemplateService,
            IHttpContextAccessor httpContextAccessor)
            : base(serviceProvider)
        {
            _publishedA2AAgentService = publishedA2AAgentService;
            _agentsTemplateService = agentsTemplateService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Returns the published A2A configuration associated with a local AgentTemplate.
        /// </summary>
        /// <param name="agentTemplateId">The local AgentTemplate identifier.</param>
        /// <returns>The published configuration, or an empty data result when none exists.</returns>
        [ApiBind]
        public async Task<AppResponseBase<PublishedA2AAgentDto>> GetByAgentTemplateId(int agentTemplateId)
        {
            return await this.GetResponseAsync<PublishedA2AAgentDto>(async (response, logger) =>
            {
                var publishedAgent = await _publishedA2AAgentService.GetByAgentTemplateIdAsync(agentTemplateId);
                if (publishedAgent == null)
                {
                    return null;
                }

                return ToResponseDto(publishedAgent);
            });
        }

        /// <summary>
        /// Creates or updates an A2A publication after validating the local Agent and public key.
        /// </summary>
        /// <param name="dto">The publication configuration.</param>
        /// <returns>A response containing the persisted publication DTO and endpoint URL.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the local agent, key, or authentication configuration is invalid.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PublishedA2AAgentDto>> SetPublishedAgent([FromBody] PublishedA2AAgentDto dto)
        {
            return await this.GetResponseAsync<PublishedA2AAgentDto>(async (response, logger) =>
            {
                Validate(dto);
                var template = await _agentsTemplateService.GetAgentTemplateAsync(dto.AgentTemplateId)
                    ?? throw new NcfExceptionBase($"未找到本地 Agent，ID：{dto.AgentTemplateId}");
                if (dto.Enable && !template.Enable)
                {
                    throw new NcfExceptionBase("本地 Agent 已停用，不能启用其对外 A2A 发布。请先启用本地 Agent。" );
                }

                var sameKey = await _publishedA2AAgentService.GetByPublicAgentKeyAsync(dto.PublicAgentKey.Trim().ToLowerInvariant());
                if (sameKey != null && sameKey.Id != dto.Id)
                {
                    throw new NcfExceptionBase("该 A2A 公开标识已经被其他本地 Agent 使用。" );
                }

                PublishedA2AAgent publishedAgent;
                if (dto.Id > 0)
                {
                    publishedAgent = await _publishedA2AAgentService.GetObjectAsync(z => z.Id == dto.Id)
                        ?? throw new NcfExceptionBase($"未找到 A2A 发布配置，ID：{dto.Id}");
                    if (publishedAgent.AgentTemplateId != dto.AgentTemplateId)
                    {
                        throw new NcfExceptionBase("已存在的 A2A 发布配置不能更换关联的本地 Agent。请先取消发布后重新创建。" );
                    }
                    publishedAgent.Update(dto);
                }
                else
                {
                    var existing = await _publishedA2AAgentService.GetByAgentTemplateIdAsync(dto.AgentTemplateId);
                    if (existing != null)
                    {
                        existing.Update(dto);
                        publishedAgent = existing;
                    }
                    else
                    {
                        publishedAgent = new PublishedA2AAgent(dto);
                    }
                }

                await _publishedA2AAgentService.SaveObjectAsync(publishedAgent);
                return ToResponseDto(publishedAgent);
            });
        }

        /// <summary>
        /// Enables or disables an existing A2A publication.
        /// </summary>
        /// <param name="id">The publication identifier.</param>
        /// <param name="enable">Whether the publication should be enabled.</param>
        /// <returns>A response containing the operation message.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the publication or its local Agent is unavailable.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Enable(int id, bool enable)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var publishedAgent = await _publishedA2AAgentService.GetObjectAsync(z => z.Id == id)
                    ?? throw new NcfExceptionBase($"未找到 A2A 发布配置，ID：{id}");
                if (enable)
                {
                    var template = await _agentsTemplateService.GetAgentTemplateAsync(publishedAgent.AgentTemplateId);
                    if (template == null || !template.Enable)
                    {
                        throw new NcfExceptionBase("本地 Agent 不可用，不能启用对外 A2A 发布。" );
                    }
                    publishedAgent.EnableAgent();
                }
                else
                {
                    publishedAgent.DisableAgent();
                }

                await _publishedA2AAgentService.SaveObjectAsync(publishedAgent);
                return $"已完成{(enable ? "启用" : "停用")} A2A 对外发布。";
            });
        }

        /// <summary>
        /// Removes an A2A publication configuration.
        /// </summary>
        /// <param name="id">The publication identifier.</param>
        /// <returns>A response containing the cancellation message.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the publication does not exist.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Delete(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var publishedAgent = await _publishedA2AAgentService.GetObjectAsync(z => z.Id == id)
                    ?? throw new NcfExceptionBase($"未找到 A2A 发布配置，ID：{id}");
                await _publishedA2AAgentService.DeleteObjectAsync(publishedAgent);
                return "已取消 A2A 对外发布。";
            });
        }

        /// <summary>
        /// Projects a published A2A Agent entity and builds its public Agent Card URL
        /// from the current HTTP request when available.
        /// </summary>
        /// <param name="publishedAgent">The published Agent configuration to project.</param>
        /// <returns>The client-facing publication DTO.</returns>
        private PublishedA2AAgentDto ToResponseDto(PublishedA2AAgent publishedAgent)
        {
            var dto = new PublishedA2AAgentDto(publishedAgent);
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                dto.AgentCardUrl = PublishedA2AAgentController.BuildEndpointUrl(request, publishedAgent.PublicAgentKey)
                    + "/.well-known/agent-card.json";
            }

            return dto;
        }

        /// <summary>
        /// Validates publication identity, public-key normalization, and inbound authentication settings.
        /// </summary>
        /// <param name="dto">The publication DTO to validate.</param>
        /// <exception cref="NcfExceptionBase">Thrown when publication or authentication values are invalid.</exception>
        private static void Validate(PublishedA2AAgentDto dto)
        {
            if (dto == null || dto.AgentTemplateId <= 0)
            {
                throw new NcfExceptionBase("请选择要发布的本地 Agent。" );
            }

            try
            {
                PublishedA2AAgent.NormalizePublicAgentKey(dto.PublicAgentKey);
            }
            catch (ArgumentException ex)
            {
                throw new NcfExceptionBase(ex.Message, ex);
            }

            if (dto.AuthenticationMode == RemoteAgentAuthenticationMode.CustomHeader
                && string.IsNullOrWhiteSpace(dto.AuthHeaderName))
            {
                throw new NcfExceptionBase("CustomHeader 鉴权必须填写请求头名称。" );
            }

            if (dto.AuthenticationMode != RemoteAgentAuthenticationMode.None
                && string.IsNullOrWhiteSpace(dto.AuthSecretKey))
            {
                throw new NcfExceptionBase("已启用鉴权，请填写部署配置中的入站密钥名。" );
            }
        }
    }
}
