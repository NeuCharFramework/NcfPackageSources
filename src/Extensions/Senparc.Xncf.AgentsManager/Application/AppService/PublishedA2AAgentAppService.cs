using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AgentsManager.OHS.Remote.Controllers;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// 本地 Agent 对外 A2A 发布配置的后台管理接口。
    /// </summary>
    [ApiAuthorize]
    public class PublishedA2AAgentAppService : AppServiceBase
    {
        private readonly PublishedA2AAgentService _publishedA2AAgentService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly IHttpContextAccessor _httpContextAccessor;

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
