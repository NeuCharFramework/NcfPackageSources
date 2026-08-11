using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// A2A 远程 Agent 管理接口。与 AgentTemplate 分开，确保旧的本地 Prompt Agent 不受影响。
    /// </summary>
    [ApiAuthorize]
    public class RemoteAgentAppService : AppServiceBase
    {
        private readonly RemoteAgentService _remoteAgentService;
        private readonly ChatGroupRemoteMemberService _chatGroupRemoteMemberService;
        private readonly RemoteA2AAgentFactory _remoteA2AAgentFactory;

        public RemoteAgentAppService(
            IServiceProvider serviceProvider,
            RemoteAgentService remoteAgentService,
            ChatGroupRemoteMemberService chatGroupRemoteMemberService,
            RemoteA2AAgentFactory remoteA2AAgentFactory)
            : base(serviceProvider)
        {
            _remoteAgentService = remoteAgentService;
            _chatGroupRemoteMemberService = chatGroupRemoteMemberService;
            _remoteA2AAgentFactory = remoteA2AAgentFactory;
        }

        [ApiBind]
        public async Task<AppResponseBase<RemoteAgent_GetListResponse>> GetList(int pageIndex = 0, int pageSize = 0, string filter = "")
        {
            return await this.GetResponseAsync<RemoteAgent_GetListResponse>(async (response, logger) =>
            {
                var expression = new SenparcExpressionHelper<RemoteAgent>();
                expression.ValueCompare.AndAlso(!string.IsNullOrWhiteSpace(filter), z => z.Name.Contains(filter));
                var list = await _remoteAgentService.GetObjectListAsync(
                    pageIndex,
                    pageSize,
                    expression.BuildWhereExpression(),
                    z => z.Id,
                    Ncf.Core.Enums.OrderingType.Descending);

                var dtoList = list.Select(z => new RemoteAgentDto(z)).ToList();
                return new RemoteAgent_GetListResponse
                {
                    List = new PagedList<RemoteAgentDto>(dtoList, list.PageIndex, list.PageCount, list.TotalCount, list.SkipCount)
                };
            });
        }

        [ApiBind]
        public async Task<AppResponseBase<RemoteAgent_GetItemResponse>> GetItem(int id)
        {
            return await this.GetResponseAsync<RemoteAgent_GetItemResponse>(async (response, logger) =>
            {
                var remoteAgent = await _remoteAgentService.GetObjectAsync(z => z.Id == id)
                    ?? throw new NcfExceptionBase($"未找到远程 Agent，ID：{id}");
                return new RemoteAgent_GetItemResponse { RemoteAgentDto = new RemoteAgentDto(remoteAgent) };
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<RemoteAgent_SetResponse>> SetRemoteAgent(RemoteAgentDto remoteAgentDto)
        {
            return await this.GetResponseAsync<RemoteAgent_SetResponse>(async (response, logger) =>
            {
                Validate(remoteAgentDto);
                RemoteAgent remoteAgent;
                if (remoteAgentDto.Id <= 0)
                {
                    remoteAgent = new RemoteAgent(remoteAgentDto);
                }
                else
                {
                    remoteAgent = await _remoteAgentService.GetObjectAsync(z => z.Id == remoteAgentDto.Id)
                        ?? throw new NcfExceptionBase($"未找到远程 Agent，ID：{remoteAgentDto.Id}");
                    remoteAgent.Update(remoteAgentDto);
                }

                await _remoteAgentService.SaveObjectAsync(remoteAgent);
                return new RemoteAgent_SetResponse { RemoteAgentDto = new RemoteAgentDto(remoteAgent) };
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> TestConnection(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var remoteAgent = await _remoteAgentService.GetObjectAsync(z => z.Id == id)
                    ?? throw new NcfExceptionBase($"未找到远程 Agent，ID：{id}");

                try
                {
                    var message = await _remoteA2AAgentFactory.TestConnectionAsync(remoteAgent);
                    remoteAgent.SetConnectionStatus(RemoteAgentConnectionStatus.Available, message);
                    await _remoteAgentService.SaveObjectAsync(remoteAgent);
                    return message;
                }
                catch (Exception ex)
                {
                    var message = $"A2A 连接失败：{ex.Message}";
                    remoteAgent.SetConnectionStatus(RemoteAgentConnectionStatus.Unavailable, message);
                    await _remoteAgentService.SaveObjectAsync(remoteAgent);
                    throw new NcfExceptionBase(message, ex);
                }
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Enable(int id, bool enable)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var remoteAgent = await _remoteAgentService.GetObjectAsync(z => z.Id == id)
                    ?? throw new NcfExceptionBase($"未找到远程 Agent，ID：{id}");
                if (enable)
                {
                    remoteAgent.EnableAgent();
                }
                else
                {
                    remoteAgent.DisableAgent();
                }

                await _remoteAgentService.SaveObjectAsync(remoteAgent);
                return $"已完成{(enable ? "启用" : "停用")}远程 Agent“{remoteAgent.Name}”";
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Delete(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var remoteAgent = await _remoteAgentService.GetObjectAsync(z => z.Id == id)
                    ?? throw new NcfExceptionBase($"未找到远程 Agent，ID：{id}");
                var groupMembers = await _chatGroupRemoteMemberService.GetFullListAsync(z => z.RemoteAgentId == id);
                if (groupMembers.Count > 0)
                {
                    throw new NcfExceptionBase("该远程 Agent 已加入 ChatGroup，需先从所有 Group 移除后才能删除。");
                }

                await _remoteAgentService.DeleteObjectAsync(remoteAgent);
                return $"已删除远程 Agent“{remoteAgent.Name}”";
            });
        }

        private static void Validate(RemoteAgentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Name))
            {
                throw new NcfExceptionBase("请填写远程 Agent 名称。");
            }

            if (!Uri.TryCreate(dto.AgentCardUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new NcfExceptionBase("请填写有效的 HTTP 或 HTTPS A2A 地址。");
            }

            if (dto.AuthenticationMode == RemoteAgentAuthenticationMode.CustomHeader
                && string.IsNullOrWhiteSpace(dto.AuthHeaderName))
            {
                throw new NcfExceptionBase("CustomHeader 鉴权必须填写请求头名称。");
            }

            if (dto.AuthenticationMode != RemoteAgentAuthenticationMode.None
                && string.IsNullOrWhiteSpace(dto.AuthSecretKey))
            {
                throw new NcfExceptionBase("已启用鉴权，请填写部署配置中的密钥名。");
            }
        }
    }
}
