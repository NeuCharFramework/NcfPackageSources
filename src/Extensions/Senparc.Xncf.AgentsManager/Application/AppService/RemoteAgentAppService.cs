/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RemoteAgentAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20260812

    Modified by: Senparc - 20260813
    Change description: v0.15.0-preview11 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

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
using Senparc.Xncf.AgentsManager.Application.Dtos;
using Senparc.Xncf.AreaBase.Admin.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// Provides authenticated APIs for configuring and testing remote A2A Agents.
    /// </summary>
    [ApiAuthorize]
    public class RemoteAgentAppService : AppServiceBase
    {
        private readonly RemoteAgentService _remoteAgentService;
        private readonly ChatGroupRemoteMemberService _chatGroupRemoteMemberService;
        private readonly RemoteA2AAgentFactory _remoteA2AAgentFactory;

        /// <summary>
        /// Initializes remote A2A agent management and ChatGroup membership services.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base application service.</param>
        /// <param name="remoteAgentService">The remote-agent persistence service.</param>
        /// <param name="chatGroupRemoteMemberService">The service used to verify group references before deletion.</param>
        /// <param name="remoteA2AAgentFactory">The factory used to test remote A2A connections.</param>
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

        /// <summary>
        /// Returns a paged list of configured remote A2A agents filtered by name.
        /// </summary>
        /// <param name="pageIndex">The zero-based page index.</param>
        /// <param name="pageSize">The page size; zero requests the service default.</param>
        /// <param name="filter">Optional name filter.</param>
        /// <returns>A response containing the paged remote-agent DTO list.</returns>
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

        /// <summary>
        /// Loads one remote A2A agent configuration.
        /// </summary>
        /// <param name="id">The remote-agent identifier.</param>
        /// <returns>A response containing the remote-agent DTO.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the identifier does not exist.</exception>
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

        /// <summary>
        /// Creates or updates a remote A2A agent after validating its URL and authentication settings.
        /// </summary>
        /// <param name="remoteAgentDto">The remote-agent configuration.</param>
        /// <returns>A response containing the persisted remote-agent DTO.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the configuration is invalid or the target does not exist.</exception>
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

        /// <summary>
        /// Tests connectivity to one configured remote A2A agent and persists its availability state.
        /// </summary>
        /// <param name="id">The remote-agent identifier.</param>
        /// <returns>A response containing the connection-test message.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the agent is missing or the connection test fails.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> TestConnection(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var remoteAgent = await _remoteAgentService.GetObjectAsync(z => z.Id == id)
                    ?? throw new NcfExceptionBase($"未找到远程 Agent，ID：{id}");
                var result = await TestRemoteAgentConnectionAsync(remoteAgent);
                if (!result.Success)
                {
                    throw new NcfExceptionBase(result.Message);
                }
                return result.Message;
            });
        }

        /// <summary>
        /// Tests either the selected remote agents or every configured remote agent and returns
        /// an individual result for each requested identifier.
        /// </summary>
        /// <param name="request">Optional remote-agent identifiers; an empty selection tests all agents.</param>
        /// <returns>A response containing one connection result per requested agent.</returns>
        /// <summary>
        /// Enables or disables a remote A2A agent configuration.
        /// </summary>
        /// <param name="id">The remote-agent identifier.</param>
        /// <param name="enable">Whether the agent should be enabled.</param>
        /// <returns>A response containing the operation message.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the remote agent does not exist.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<RemoteAgent_TestConnectionsResponse>> TestConnections(
            [FromBody] RemoteAgent_TestConnectionsRequest request)
        {
            return await this.GetResponseAsync<RemoteAgent_TestConnectionsResponse>(async (response, logger) =>
            {
                var requestedIds = request?.RemoteAgentIds?
                    .Where(z => z > 0)
                    .Distinct()
                    .ToList() ?? new List<int>();
                var result = new RemoteAgent_TestConnectionsResponse();

                if (requestedIds.Count == 0)
                {
                    var allAgents = await _remoteAgentService.GetFullListAsync(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);
                    foreach (var remoteAgent in allAgents)
                    {
                        result.Results.Add(await TestRemoteAgentConnectionAsync(remoteAgent));
                    }
                    return result;
                }

                var remoteAgents = await _remoteAgentService.GetFullListAsync(z => requestedIds.Contains(z.Id));
                var remoteAgentMap = remoteAgents.ToDictionary(z => z.Id);
                foreach (var remoteAgentId in requestedIds)
                {
                    if (!remoteAgentMap.TryGetValue(remoteAgentId, out var remoteAgent))
                    {
                        result.Results.Add(new RemoteAgent_ConnectionTestResult
                        {
                            RemoteAgentId = remoteAgentId,
                            Success = false,
                            Message = $"未找到远程 Agent，ID：{remoteAgentId}"
                        });
                        continue;
                    }

                    result.Results.Add(await TestRemoteAgentConnectionAsync(remoteAgent));
                }
                return result;
            });
        }

        /// <summary>
        /// Enables or disables one remote A2A agent and persists the new availability state.
        /// </summary>
        /// <param name="id">The remote-agent identifier.</param>
        /// <param name="enable">Whether the agent should be available for selection.</param>
        /// <returns>A response containing the state-transition message.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the remote agent does not exist.</exception>
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

        /// <summary>
        /// Deletes a remote A2A agent that is not referenced by any ChatGroup.
        /// </summary>
        /// <param name="id">The remote-agent identifier.</param>
        /// <returns>A response containing the deletion message.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the agent is missing or still belongs to a ChatGroup.</exception>
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

        /// <summary>
        /// Validates the remote agent URL and authentication-mode requirements.
        /// </summary>
        /// <param name="dto">The configuration to validate.</param>
        /// <exception cref="NcfExceptionBase">Thrown when required values or authentication settings are invalid.</exception>
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

        /// <summary>
        /// Tests a remote A2A connection, persists the resulting availability state,
        /// and converts success or failure into the application response DTO.
        /// </summary>
        /// <param name="remoteAgent">The remote Agent configuration to test.</param>
        /// <returns>The connection test result and updated remote-agent DTO.</returns>
        private async Task<RemoteAgent_ConnectionTestResult> TestRemoteAgentConnectionAsync(RemoteAgent remoteAgent)
        {
            try
            {
                var message = await _remoteA2AAgentFactory.TestConnectionAsync(remoteAgent);
                remoteAgent.SetConnectionStatus(RemoteAgentConnectionStatus.Available, message);
                await _remoteAgentService.SaveObjectAsync(remoteAgent);
                return new RemoteAgent_ConnectionTestResult
                {
                    RemoteAgentId = remoteAgent.Id,
                    Name = remoteAgent.Name,
                    Success = true,
                    Message = message,
                    RemoteAgentDto = new RemoteAgentDto(remoteAgent)
                };
            }
            catch (Exception ex)
            {
                var message = $"A2A 连接失败：{ex.Message}";
                remoteAgent.SetConnectionStatus(RemoteAgentConnectionStatus.Unavailable, message);
                try
                {
                    await _remoteAgentService.SaveObjectAsync(remoteAgent);
                }
                catch (Exception saveException)
                {
                    message = $"{message}；无法保存连接状态：{saveException.Message}";
                }

                return new RemoteAgent_ConnectionTestResult
                {
                    RemoteAgentId = remoteAgent.Id,
                    Name = remoteAgent.Name,
                    Success = false,
                    Message = message,
                    RemoteAgentDto = new RemoteAgentDto(remoteAgent)
                };
            }
        }
    }
}
