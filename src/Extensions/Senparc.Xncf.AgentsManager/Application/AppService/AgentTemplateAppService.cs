using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Client;
using Senparc.CO2NET;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Models.Entities;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.CO2NET.Extensions;


namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    public class AgentTemplateAppService : AppServiceBase
    {
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;

        public AgentTemplateAppService(IServiceProvider serviceProvider, AgentsTemplateService agentsTemplateService, PromptItemService promptItemService, PromptRangeService promptRangeService) : base(serviceProvider)
        {
            this._agentsTemplateService = agentsTemplateService;
            this._promptItemService = promptItemService;
            this._promptRangeService = promptRangeService;
        }

        //[ApiBind]
        [FunctionRender("Agent 模板管理", "Agent 模板管理", typeof(Register))]
        public async Task<StringAppResponse> AgentTemplateManage(AgentTemplate_ManageRequest request)
        {
            Console.Write(request.ToJson(true));
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                SenparcAI_GetByVersionResponse promptResult;
                var promptCode = await NormalizePromptCodeAsync(request.GetSystemMessagePromptCode());

                try
                {
                    //检查 PromptCode 是否存在
                    promptResult = await _promptItemService.GetWithVersionAsync(promptCode, isAvg: true);
                }
                catch (Exception ex)
                {
                    // Prompt Code不存在的时候，会抛出异常
                    return ex.Message;
                }

                var promptTemplate = promptResult.PromptItem.Content;// Prompt

                var agentTemplateDto = new AgentTemplateDto(request.Name, promptCode, true,
                    request.Description, promptCode,
                    Enum.Parse<HookRobotType>(request.HookRobotType), request.HookRobotParameter, request.FunctionCallNames);

                await this._agentsTemplateService.UpdateAgentTemplateAsync(request.Id, agentTemplateDto);

                logger.Append("Agent 模板更新成功！");
                logger.Append("当前代理使用的 Prompt 模板：" + promptTemplate);

                return logger.ToString();
            });
        }

//[ApiBind]
        [FunctionRender("从 PromptCode 快速创建智能体", "根据 PromptCode 快速创建智能体。支持靶场级别（如：RangeName）、靶道级别（如：RangeName-T1）、完整定位（如：RangeName-T1-A1）", typeof(Register))]
        public async Task<StringAppResponse> CreateAgentFromPromptCode(AgentTemplate_CreateFromPromptCodeRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
             try{
            Console.Write(request.ToJson(true));
                var promptCode = request.GetPromptCode();//await NormalizePromptCodeAsync(request.GetPromptCode());

                if (string.IsNullOrEmpty(promptCode))
                {
                    return "请选择或手动输入 PromptCode";
                }

                if (string.IsNullOrEmpty(request.Name))
                {
                    return "请输入智能体名称";
                }

                // 检查是否已有使用该 PromptCode 前缀的智能体
                var existingAgents = await this._agentsTemplateService.GetObjectListAsync(0, 0,
                    z => z.PromptCode != null && z.PromptCode.StartsWith(promptCode),
                    z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                if (existingAgents.TotalCount > 0)
                {
                    var existingNames = string.Join("、", existingAgents.Select(z => z.Name));
                    logger.Append($"⚠️ 注意：当前 PromptCode（{promptCode}）已有 {existingAgents.TotalCount} 个智能体使用：{existingNames}");
                    logger.Append("已继续创建新智能体。");
                }

                var agentTemplateDto = new AgentTemplateDto(request.Name, promptCode, true,
                    request.Description ?? "", promptCode,
                    HookRobotType.None, "", null, request.FunctionCallNames);

                await this._agentsTemplateService.UpdateAgentTemplateAsync(0, agentTemplateDto);

                logger.Append($"✅ 智能体「{request.Name}」创建成功！");
                logger.Append($"使用的 PromptCode：{promptCode}");
             }catch(Exception ex){

logger.Append($"❌ 创建智能体失败：{ex.Message}");
             }
                return logger.ToString();
            });
        }

        [FunctionRender("搜索 Agent 模板并返回 ID", "根据名称或 PromptCode 搜索最匹配的 AgentTemplate，并返回可选 ID。支持多个关键词。", typeof(Register))]
        public async Task<StringAppResponse> FindAgentTemplate(AgentTemplate_FindByNameRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return "请输入搜索词（名称、PromptCode 或关键字）";
                }

                var topN = request.TopN <= 0 ? 5 : Math.Min(request.TopN, 20);
                var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["提示词优化器"] = "PromptCatalyzer",
                    ["优化器"] = "PromptCatalyzer"
                };

                var keywords = request.Query
                    .Split(new[] { ',', '，', ';', '；', '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(z => z.Trim())
                    .Where(z => !string.IsNullOrWhiteSpace(z))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (keywords.Count == 0)
                {
                    return "请输入有效搜索词";
                }

                var enabledAgents = await _agentsTemplateService.GetFullListAsync(z => z.Enable, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                foreach (var keywordRaw in keywords)
                {
                    var keyword = aliasMap.TryGetValue(keywordRaw, out var alias) ? alias : keywordRaw;
                    var exact = enabledAgents
                        .Where(z => string.Equals(z.Name, keyword, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(z.PromptCode, keyword, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(z => z.Id)
                        .ToList();

                    var fuzzy = enabledAgents
                        .Where(z =>
                            (!string.IsNullOrWhiteSpace(z.Name) && z.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            || (!string.IsNullOrWhiteSpace(z.PromptCode) && z.PromptCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                        .OrderByDescending(z => z.Id)
                        .ToList();

                    var candidates = exact.Count > 0
                        ? exact
                        : fuzzy;

                    logger.Append($"关键词：{keywordRaw}");
                    if (candidates.Count == 0)
                    {
                        logger.Append("  未找到可用 AgentTemplate");
                        continue;
                    }

                    foreach (var c in candidates.Take(topN))
                    {
                        logger.Append($"  ID={c.Id} | 名称={c.Name} | PromptCode={c.PromptCode}");
                    }
                }

                return logger.ToString();
            });
        }

        /// <summary>
        /// 将靶场别称开头的 PromptCode 归一化为 RangeName 开头，避免把 Alias 存入 SystemMessage。
        /// </summary>
        private async Task<string> NormalizePromptCodeAsync(string promptCode)
        {
            if (string.IsNullOrWhiteSpace(promptCode))
            {
                return promptCode;
            }

            var normalizedPromptCode = promptCode.Trim();
            var splitIndex = normalizedPromptCode.IndexOf('-');
            var rangePrefix = splitIndex >= 0 ? normalizedPromptCode.Substring(0, splitIndex) : normalizedPromptCode;
            var suffix = splitIndex >= 0 ? normalizedPromptCode.Substring(splitIndex) : string.Empty;

            var promptRange = await _promptRangeService.GetObjectAsync(z => z.RangeName == rangePrefix || z.Alias == rangePrefix);
            if (promptRange == null || string.IsNullOrWhiteSpace(promptRange.RangeName))
            {
                return normalizedPromptCode;
            }

            return promptRange.RangeName + suffix;
        }

        /// <summary>
        /// 获取 AgentTemplate 的列表
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<AgentTemplate_GetListResponse>> GetList(int pageIndex = 0, int pageSize = 0, string filter = "")
        {
            return await this.GetResponseAsync<AgentTemplate_GetListResponse>(async (response, logger) =>
            {
                var seh = new SenparcExpressionHelper<Models.DatabaseModel.AgentTemplate>();
                seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(filter), _ => _.Name.Contains(filter));
                var where = seh.BuildWhereExpression();
                var list = await this._agentsTemplateService.GetObjectListAsync(pageIndex, pageSize, where, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                var chatGroupMemberService = base.GetRequiredService<ChatGroupMemberService>();
                var chatTaskService = base.GetRequiredService<ChatTaskService>();

                var agentIds = list.Select(z => z.Id).Distinct().ToList();
                var groupMembers = agentIds.Count > 0
                    ? await chatGroupMemberService.GetFullListAsync(z => agentIds.Contains(z.AgentTemplateId))
                    : new List<Models.DatabaseModel.Models.ChatGroupMember>();

                var groupIds = groupMembers.Select(z => z.ChatGroupId).Distinct().ToList();
                var activeTasks = groupIds.Count > 0
                    ? await chatTaskService.GetFullListAsync(z =>
                        groupIds.Contains(z.ChatGroupId)
                        && (z.Status == ChatTask_Status.Waiting
                            || z.Status == ChatTask_Status.Chatting
                            || z.Status == ChatTask_Status.Paused))
                    : new List<ChatTask>();

                var activeTaskCountByGroup = activeTasks
                    .GroupBy(z => z.ChatGroupId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var promptScoreCache = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                var dtoList = new List<AgentTemplateSimpleStatusDto>();

                foreach (var item in list)
                {
                    var dto = _agentsTemplateService.Mapping<AgentTemplateSimpleStatusDto>(item);

                    var memberGroupIds = groupMembers
                        .Where(z => z.AgentTemplateId == item.Id)
                        .Select(z => z.ChatGroupId)
                        .Distinct()
                        .ToList();

                    dto.ChattingCount = memberGroupIds.Sum(groupId =>
                        activeTaskCountByGroup.TryGetValue(groupId, out var count) ? count : 0);

                    dto.Score = await GetAgentScoreByPromptCodeAsync(dto.PromptCode, promptScoreCache);
                    dtoList.Add(dto);
                }

                var listDto = new PagedList<AgentTemplateSimpleStatusDto>(dtoList,
                    list.PageIndex, list.PageCount, list.TotalCount, list.SkipCount);

                var result = new AgentTemplate_GetListResponse()
                {
                    List = listDto
                };
                return result;
            });
        }

        private async Task<float> GetAgentScoreByPromptCodeAsync(string promptCode, Dictionary<string, float> scoreCache)
        {
            if (string.IsNullOrWhiteSpace(promptCode))
            {
                return -1;
            }

            if (scoreCache.TryGetValue(promptCode, out var cachedScore))
            {
                return cachedScore;
            }

            try
            {
                var promptItem = await _promptItemService.GetBestPromptAsync(promptCode, true);
                var score = promptItem == null ? -1 : (float)promptItem.EvalAvgScore;
                scoreCache[promptCode] = score;
                return score;
            }
            catch
            {
                scoreCache[promptCode] = -1;
                return -1;
            }
        }

        /// <summary>
        /// 获取 PromptRange 的树状结构
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<PromptItemTreeList>> GetPromptRangeTree()
        {
            return await this.GetResponseAsync<PromptItemTreeList>(async (response, logger) =>
           {
               var items = await _promptItemService.GetPromptRangeTreeList(true, true);
               return items;
           });
        }

        /// <summary>
        /// 创建或更新 AgentTemplate
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AgentTemplateDto>> SetItem([FromBody] AgentTemplateDto_UpdateOrCreate agentTemplateDto)
        {
            //if (!ModelState.IsValid)
            //{
            //    // Log the model state errors  
            //    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            //    {
            //        Console.WriteLine(error.ErrorMessage);
            //    }

            //    return BadRequest(ModelState);
            //}

            return await this.GetResponseAsync<AgentTemplateDto>(async (response, logger) =>
            {
                var newDto = await this._agentsTemplateService.UpdateAgentTemplateAsync(agentTemplateDto.Id, agentTemplateDto);
                return newDto;
            });
        }

        /// <summary>
        /// 获取 AgentTemplate 的详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<AgentTemplate_GetItemResponse>> GetItem(int id)
        {
            return await this.GetResponseAsync<AgentTemplate_GetItemResponse>(async (response, logger) =>
            {
                var agentTemplate = await this._agentsTemplateService.GetObjectAsync(z => z.Id == id, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                var dto = this._agentsTemplateService.Mapping<AgentTemplateDto>(agentTemplate);
                var result = new AgentTemplate_GetItemResponse()
                {
                    AgentTemplate = dto,
                };

                return result;
            });
        }

        /// <summary>
        /// 获取带状态的 AgentTemplate 的详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<AgentTemplate_GetItemStatusResponse>> GetItemStatus(int id)
        {
            return await this.GetResponseAsync<AgentTemplate_GetItemStatusResponse>(async (response, logger) =>
            {
                var agentTemplate = await this._agentsTemplateService.GetObjectAsync(z => z.Id == id, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                var agentTemplateDto = this._agentsTemplateService.Mapping<AgentTemplateDto>(agentTemplate);

                var promptCode = agentTemplateDto.PromptCode;
                var promptItem = await this._promptItemService.GetBestPromptAsync(promptCode, true);
                var promptItemDto = this._promptItemService.Mapping<PromptItemDto>(promptItem);

                var promptRangeDto = await _promptRangeService.GetAsync(promptItem.RangeId);
                promptItemDto.PromptRange = promptRangeDto;

                var aiModelService = base.GetService<AIModelService>();
                var aiModel = await aiModelService.GetObjectAsync(z => z.Id == promptItem.ModelId);
                var aiModelDto = aiModelService.Mapping<AIModelDto>(aiModel);

                var result = new AgentTemplate_GetItemStatusResponse()
                {
                    AgentTemplateStatus = new AgentTemplateStatusDto()
                    {
                        AgentTemplateDto = agentTemplateDto,
                        PromptItemDto = promptItemDto,
                        PromptRangeDto = promptRangeDto,
                        AIModelDto = aiModelDto
                    }
                };

                return result;
            });
        }

        /// <summary>
        /// 启用或者停用 AgentTemplate
        /// </summary>
        /// <param name="id">AgentTemplate ID</param>
        /// <param name="enable">是否启用</param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Enable(int id, bool enable)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var agent = await this._agentsTemplateService.GetAgentTemplateAsync(id);
                if (enable)
                {
                    agent.EnableAgent();
                }
                else
                {
                    agent.DisableAgent();
                }
                await this._agentsTemplateService.SaveObjectAsync(agent);

                return $"已完成{(enable ? "启用" : "停用")}";
            });
        }

        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> Delete(int id)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteInternalAsync(new List<int> { id }, logger);
                return result;
            });
        }

        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> DeleteBatch([FromBody] List<int> ids)
        {
            return await this.GetResponseAsync<string>(async (response, logger) =>
            {
                var result = await DeleteInternalAsync(ids, logger);
                return result;
            });
        }

/// <summary>
        /// 根据 PromptCode 前缀获取匹配的 AgentTemplate 列表
        /// </summary>
        /// <param name="promptCode">PromptCode（支持前缀匹配，如"RangeName"、"RangeName-T1"、"RangeName-T1-A1"）</param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<List<AgentTemplateSimpleStatusDto>>> GetListByPromptCode(string promptCode)
        {
            return await this.GetResponseAsync<List<AgentTemplateSimpleStatusDto>>(async (response, logger) =>
            {
                if (string.IsNullOrEmpty(promptCode))
                {
                    return new List<AgentTemplateSimpleStatusDto>();
                }

                var list = await this._agentsTemplateService.GetObjectListAsync(0, 0,
                    z => z.PromptCode != null && z.PromptCode.StartsWith(promptCode),
                    z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                var result = list.Select(z =>
                    _agentsTemplateService.Mapping<AgentTemplateSimpleStatusDto>(z)).ToList();

                return result;
            });
        }

        /// <summary>
        /// 获取所有已注册的 AI Plugin 类型
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<List<string>>> GetPluginTypes()
        {
            return await this.GetResponseAsync<List<string>>((response, logger) =>
            {
                var pluginTypes = AIPluginHub.Instance.GetAllPluginNames();
                return Task.FromResult(pluginTypes);
            });
        }

        /// <summary>
        /// 测试MCP连接
        /// </summary>
        /// <param name="endpointName">Endpoint名称</param>
        /// <param name="endpointUrl">Endpoint URL</param>
        /// <returns>包含工具列表和连接状态的响应</returns>
        [ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Get)]
        public async Task<AppResponseBase<McpConnectionTestResult>> TestMcpConnection(string endpointName, string endpointUrl)
        {
            return await this.GetResponseAsync<McpConnectionTestResult>(async (response, logger) =>
            {
                List<McpTool> mcpToolList = new List<McpTool>();
                try
                {
                    var clientTransport = new SseClientTransport(new SseClientTransportOptions()
                    {
                        Endpoint = new Uri(endpointUrl),
                        Name = endpointName
                    });

                    await using var client = await McpClientFactory.CreateAsync(clientTransport);
                    var tools = await client.ListToolsAsync();

                    mcpToolList = tools.Select(z => new McpTool()
                    {
                        Name = z.Name,
                        Description = z.Description,
                        Parameters = z.AdditionalProperties.Select(z => new McpToolParameter()
                        {
                            Name = z.Key,
                            Description = z.Value.ToString()
                        }).ToList()
                    }).ToList();

                    await clientTransport.DisposeAsync();

                    return new McpConnectionTestResult()
                    {
                        Success = true,
                        Status = 200,
                        StatusMessage = "连接成功",
                        Tools = mcpToolList
                    };

                }
                catch (System.Text.Json.JsonException ex)
                {
                    logger.Append($"解析工具列表时出错: {ex.Message}");
                    // 创建一个假工具以显示错误信息
                    mcpToolList.Add(new McpTool
                    {
                        Name = "解析错误",
                        Description = $"无法解析工具列表: {ex.Message}"
                    });

                    return new McpConnectionTestResult()
                    {
                        Success = false,
                        Status = 500,
                        StatusMessage = "连接失败",
                        Tools = mcpToolList
                    };
                }

            });
        }

        private async Task<string> DeleteInternalAsync(List<int> ids, AppServiceLogger logger)
        {
            if (ids == null || ids.Count == 0)
            {
                return "未提供 Agent ID";
            }

            var idSet = ids.Distinct().ToList();
            var chatGroupService = base.GetRequiredService<ChatGroupService>();
            var chatGroupMemberService = base.GetRequiredService<ChatGroupMemberService>();
            var chatGroupHistoryService = base.GetRequiredService<ChatGroupHistoryService>();

            var groupsAsRole = await chatGroupService.GetFullListAsync(
                z => idSet.Contains(z.AdminAgentTemplateId) || idSet.Contains(z.EnterAgentTemplateId));

            var blockedByRoleMap = groupsAsRole
                .SelectMany(group =>
                {
                    var pairs = new List<(int agentId, string message)>();
                    if (idSet.Contains(group.AdminAgentTemplateId))
                    {
                        pairs.Add((group.AdminAgentTemplateId, $"Agent 被组【{group.Name}】作为群主引用"));
                    }
                    if (idSet.Contains(group.EnterAgentTemplateId))
                    {
                        pairs.Add((group.EnterAgentTemplateId, $"Agent 被组【{group.Name}】作为对接人引用"));
                    }
                    return pairs;
                })
                .GroupBy(z => z.agentId)
                .ToDictionary(g => g.Key, g => g.Select(z => z.message).Distinct().ToList());

            var deleted = 0;
            var blocked = 0;
            var missing = 0;

            foreach (var id in idSet)
            {
                var agent = await _agentsTemplateService.GetObjectAsync(z => z.Id == id);
                if (agent == null)
                {
                    missing++;
                    continue;
                }

                if (blockedByRoleMap.TryGetValue(id, out var blockedMessages) && blockedMessages.Count > 0)
                {
                    blocked++;
                    logger.Append($"✗ 阻止删除 Agent【{agent.Name}】：{string.Join("；", blockedMessages)}");
                    continue;
                }

                // 移除普通成员关系（不影响群主/对接人引用，因为上面已经阻止）
                var members = await chatGroupMemberService.GetFullListAsync(z => z.AgentTemplateId == id);
                foreach (var member in members)
                {
                    await chatGroupMemberService.DeleteObjectAsync(member);
                }

                // 删除与该 Agent 相关的历史消息，避免外键约束冲突
                var histories = await chatGroupHistoryService.GetFullListAsync(
                    z => z.FromAgentTemplateId == id || z.ToAgentTemplateId == id);
                foreach (var history in histories)
                {
                    await chatGroupHistoryService.DeleteObjectAsync(history);
                }

                await _agentsTemplateService.DeleteObjectAsync(agent);
                deleted++;
                logger.Append($"✓ 已删除 Agent【{agent.Name}】（成员关系 {members.Count} 条，消息记录 {histories.Count} 条）");
            }

            logger.Append($"删除 Agent 完成：成功 {deleted}，阻止 {blocked}，不存在 {missing}");
            return logger.ToString();
        }
    }

    /// <summary>
    /// MCP连接测试结果
    /// </summary>
    public class McpConnectionTestResult
    {
        /// <summary>
        /// 连接是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// 工具列表
        /// </summary>
        public List<McpTool> Tools { get; set; } = new List<McpTool>();
    }

    /// <summary>
    /// MCP工具信息
    /// </summary>
    public class McpTool
    {
        /// <summary>
        /// 工具名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 工具描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 工具参数列表
        /// </summary>
        public List<McpToolParameter> Parameters { get; set; } = new List<McpToolParameter>();
    }

    /// <summary>
    /// MCP工具参数
    /// </summary>
    public class McpToolParameter
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; set; }
    }
}
