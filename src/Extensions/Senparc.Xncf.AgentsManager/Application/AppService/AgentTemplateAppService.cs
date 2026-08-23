/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentTemplateAppService.cs
    文件功能描述：AgentTemplateAppService 服务逻辑

    创建标识：Senparc - 20240616

    修改标识：Senparc - 20260701
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

    修改标识：Senparc - 20260717
    修改描述：v0.12.0-preview6 为 AgentsManager 模块接入统一资源本地化并优化功能文案

    修改标识：Senparc - 20260804
    修改描述：v0.14.0-preview9 新增 Agent 模板知识库关联与管理统计

    修改标识：Senparc - 20260813
    修改描述：v0.15.0-preview11 增强 A2A 智能体、ChatGroup 执行能力与管理界面

    修改标识：Senparc - 20260815
    修改描述：v0.15.0-preview20 增强 AgentTemplate、ChatGroup 与发布型 A2A 的取消和请求处理

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 Human-in-the-Loop 人工审批与人类参与者执行策略

    修改标识：Senparc - 20260817
    修改描述：v0.16.0 支持 AgentTemplate 模型绑定、空输出 Token 重试与 Human-in-the-Loop

    修改标识：Senparc - 20260822
    修改描述：v0.16.0 增强 Agent 工作流校验、函数绑定与任务管理交互

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.Usage;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AgentsManager.Abstractions;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Models.Entities;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using Senparc.Xncf.KnowledgeBase.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    [ApiAuthorize]
    public class AgentTemplateAppService : AppServiceBase
    {
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly IAgentWorkflowReferenceValidator _agentWorkflowReferenceValidator;
        private readonly IWorkflowFunctionCallingProvider _workflowFunctionCallingProvider;

        public AgentTemplateAppService(
            IServiceProvider serviceProvider,
            AgentsTemplateService agentsTemplateService,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            IEnumerable<IAgentWorkflowReferenceValidator> agentWorkflowReferenceValidators = null,
            IEnumerable<IWorkflowFunctionCallingProvider> workflowFunctionCallingProviders = null) : base(serviceProvider)
        {
            this._agentsTemplateService = agentsTemplateService;
            this._promptItemService = promptItemService;
            this._promptRangeService = promptRangeService;
            _agentWorkflowReferenceValidator = agentWorkflowReferenceValidators?.FirstOrDefault();
            _workflowFunctionCallingProvider = workflowFunctionCallingProviders?.FirstOrDefault();
        }

        //[ApiBind]
        [FunctionRender(typeof(AgentsManagerResource), "Function.Agents.TemplateManagement.Name", "Function.Agents.TemplateManagement.Description", typeof(Register))]
        public async Task<StringAppResponse> AgentTemplateManage(AgentTemplate_ManageRequest request)
        {
            Console.Write(request.ToJson(true));
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                var requestedPrompt = request.GetSystemMessagePromptCode();
                var promptCode = AgentTemplateRunner.IsPromptRangeReference(requestedPrompt)
                    ? await NormalizePromptCodeAsync(requestedPrompt)
                    : requestedPrompt;
                var promptTemplate = promptCode;

                var existingAgent = request.Id > 0
                    ? await _agentsTemplateService.GetObjectAsync(z => z.Id == request.Id)
                    : null;
                if ((existingAgent?.IsHuman ?? false)
                    || HumanParticipantConstants.IsHuman(promptCode))
                {
                    return "Human 是系统保留的特殊参与者，不能通过普通 Agent 接口创建或修改。";
                }

                if (AgentTemplateRunner.IsPromptRangeReference(promptCode))
                {
                    try
                    {
                        // 只有 PromptRange 版本引用才需要解析；普通文本直接作为 System Message。
                        var promptResult = await _promptItemService.GetWithVersionAsync(promptCode, isAvg: true);
                        promptTemplate = promptResult.PromptItem.Content;
                    }
                    catch (Exception ex)
                    {
                        // Prompt Code 不存在的时候，会抛出异常。
                        return ex.Message;
                    }
                }

                await ValidateKnowledgeBaseBindingAsync(request.KnowledgeBaseId);
                var agentTemplateDto = new AgentTemplateDto(request.Name, promptCode, true,
                    request.Description, promptCode,
                    Enum.Parse<HookRobotType>(request.HookRobotType), request.HookRobotParameter,
                    null, request.FunctionCallNames, null, request.KnowledgeBaseId);

                await this._agentsTemplateService.UpdateAgentTemplateAsync(request.Id, agentTemplateDto);

                logger.Append("Agent 模板更新成功！");
                logger.Append("当前代理使用的 Prompt 模板：" + promptTemplate);

                return logger.ToString();
            });
        }

//[ApiBind]
        [FunctionRender(typeof(AgentsManagerResource), "Function.Agents.CreateFromPromptCode.Name", "Function.Agents.CreateFromPromptCode.Description", typeof(Register))]
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

                if (HumanParticipantConstants.IsHuman(promptCode))
                {
                    return "Human 是系统保留的特殊参与者，不能通过普通 Agent 接口创建。";
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

        [FunctionRender(typeof(AgentsManagerResource), "Function.Agents.SearchTemplate.Name", "Function.Agents.SearchTemplate.Description", typeof(Register))]
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
                        logger.Append($"  ID={c.Id} | 名称={c.Name} | PromptCode={c.PromptCode}{System.Environment.NewLine}");
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
                var publishedA2AAgentService = base.GetRequiredService<PublishedA2AAgentService>();

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
                var publishedA2AAgents = agentIds.Count > 0
                    ? (await publishedA2AAgentService.GetFullListAsync(z => agentIds.Contains(z.AgentTemplateId))).ToList()
                    : new List<Models.DatabaseModel.Models.PublishedA2AAgent>();
                // AgentTemplateId is logically unique. Keep the list query resilient to any historical duplicate data.
                var publishedA2AByAgentId = publishedA2AAgents
                    .GroupBy(z => z.AgentTemplateId)
                    .ToDictionary(group => group.Key, group => group.OrderByDescending(z => z.Id).First());

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
                    if (publishedA2AByAgentId.TryGetValue(item.Id, out var publishedA2A))
                    {
                        dto.HasPublishedA2A = true;
                        dto.PublishedA2AEnabled = publishedA2A.Enable;
                    }
                    dtoList.Add(dto);
                }

                await PopulateAgentMetadataAsync(dtoList);

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

            if (!AgentTemplateRunner.IsPromptRangeReference(promptCode))
            {
                scoreCache[promptCode] = -1;
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
                var existingAgent = agentTemplateDto.Id > 0
                    ? await _agentsTemplateService.GetObjectAsync(z => z.Id == agentTemplateDto.Id)
                    : null;
                if (existingAgent?.IsHuman ?? false)
                {
                    await ValidateAgentModelBindingAsync(agentTemplateDto, isHumanParticipant: true);
                    existingAgent.UpdateModelBinding(agentTemplateDto.ModelBinding, agentTemplateDto.AiModelId);
                    await _agentsTemplateService.SaveObjectAsync(existingAgent);

                    var humanDto = _agentsTemplateService.Mapping<AgentTemplateDto>(existingAgent);
                    await PopulateAgentMetadataAsync(new[] { humanDto });
                    return humanDto;
                }

                if (HumanParticipantConstants.IsHuman(agentTemplateDto.PromptCode))
                {
                    response.Success = false;
                    response.ErrorMessage = "Human 是系统保留的特殊参与者，不能通过普通 Agent 接口创建。";
                    return null;
                }

                await ValidateAgentModelBindingAsync(agentTemplateDto);
                await ValidateKnowledgeBaseBindingAsync(agentTemplateDto.KnowledgeBaseId);
                agentTemplateDto.FunctionBindings = AgentFunctionBindingCodec
                    .Normalize(agentTemplateDto.FunctionBindings)
                    .ToList();
                agentTemplateDto.FunctionCallNames = AgentFunctionBindingCodec.Serialize(
                    agentTemplateDto.FunctionBindings,
                    agentTemplateDto.FunctionCallNames);

                if (_agentWorkflowReferenceValidator != null && agentTemplateDto.Id > 0)
                {
                    var bindingError = await _agentWorkflowReferenceValidator.ValidateAgentBindingsAsync(
                        agentTemplateDto.Id,
                        GetCurrentAdminUserId(),
                        agentTemplateDto.FunctionBindings
                            .Where(AgentFunctionBindingCodec.IsWorkflowBinding)
                            .Select(binding => binding.WorkflowId > 0
                                ? binding.WorkflowId.Value
                                : int.TryParse(binding.Key, out var workflowId) ? workflowId : 0)
                            .Where(workflowId => workflowId > 0)
                            .Distinct()
                            .ToList(),
                        default);
                    if (bindingError != null)
                    {
                        response.Success = false;
                        response.ErrorMessage = bindingError;
                        return null;
                    }
                }

                var newDto = await this._agentsTemplateService.UpdateAgentTemplateAsync(agentTemplateDto.Id, agentTemplateDto);
                await PopulateAgentMetadataAsync(new[] { newDto });
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
                await PopulateAgentMetadataAsync(new[] { dto });
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
                await PopulateAgentMetadataAsync(new[] { agentTemplateDto });

                var promptCode = agentTemplateDto.PromptCode;
                PromptItemDto promptItemDto = null;
                PromptRangeDto promptRangeDto = null;
                AIModelDto aiModelDto = null;
                var aiModelService = base.GetService<AIModelService>();

                // PromptCode 兼容两种数据：PromptRange 版本号，或用户手动输入的 SystemMessage。
                // 手动 Prompt 没有关联 PromptItem，不能直接交给 GetBestPromptAsync，否则会被当作
                // RangeName 查询并返回“找不到对应的靶场”。
                if (agentTemplateDto.ModelBinding == AgentModelBindingMode.ManualAiModel
                    && agentTemplateDto.AiModelId > 0)
                {
                    var aiModel = await aiModelService.GetObjectAsync(z => z.Id == agentTemplateDto.AiModelId.Value);
                    aiModelDto = aiModelService.Mapping<AIModelDto>(aiModel);
                }
                else if (AgentTemplateRunner.IsPromptRangeReference(promptCode))
                {
                    var promptItem = await this._promptItemService.GetBestPromptAsync(promptCode.Trim(), true);
                    promptItemDto = this._promptItemService.Mapping<PromptItemDto>(promptItem);

                    promptRangeDto = await _promptRangeService.GetAsync(promptItem.RangeId);
                    promptItemDto.PromptRange = promptRangeDto;

                    var aiModel = await aiModelService.GetObjectAsync(z => z.Id == promptItem.ModelId);
                    aiModelDto = aiModelService.Mapping<AIModelDto>(aiModel);
                }

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

        [ApiBind]
        public async Task<AppResponseBase<AgentFunctionBindingCatalogResponse>> GetFunctionBindingCatalog(int agentId = 0)
        {
            return await this.GetResponseAsync<AgentFunctionBindingCatalogResponse>(async (response, logger) =>
            {
                var result = new AgentFunctionBindingCatalogResponse();
                var currentAgent = agentId > 0
                    ? await _agentsTemplateService.GetObjectAsync(item => item.Id == agentId)
                    : null;
                result.CurrentBindings = AgentFunctionBindingCodec.Parse(currentAgent?.FunctionCallNames)
                    .Select(CloneBinding)
                    .ToList();

                var moduleService = base.GetRequiredService<XncfModuleService>();
                foreach (var register in XncfRegisterManager.RegisterList)
                {
                    var module = await moduleService.GetObjectAsync(item => item.Uid == register.Uid);
                    var available = module?.State == XncfModules_State.开放;
                    if (!Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.TryGetValue(register.GetType(), out var group))
                    {
                        continue;
                    }

                    foreach (var bag in group.Values
                        .Where(item => item.MethodInfo != null && item.FunctionRenderAttribute.AllowAiInvocation)
                        .GroupBy(item => item.Key)
                        .Select(items => items.First()))
                    {
                        result.Functions.Add(new AgentFunctionBindingOptionResponse
                        {
                            Kind = "function",
                            Key = $"{register.Uid}::{bag.Key}",
                            Name = bag.FunctionRenderAttribute.Name,
                            Description = bag.FunctionRenderAttribute.Description,
                            ModuleUid = register.Uid,
                            ModuleName = register.MenuName,
                            ModuleVersion = register.Version,
                            FunctionKey = bag.Key,
                            ParameterCount = bag.MethodInfo.GetParameters().Length,
                            Available = available
                        });
                    }
                }

                result.Plugins = AIPluginHub.Instance.GetAllPluginNames()
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .OrderBy(name => name)
                    .Select(name => new AgentFunctionBindingOptionResponse
                    {
                        Kind = "plugin",
                        Key = name,
                        Name = name,
                        Description = "兼容旧版 Agent Plugin Function Calling 绑定。",
                        Available = true
                    })
                    .ToList();

                var adminUserId = GetCurrentAdminUserId();
                if (_workflowFunctionCallingProvider != null && adminUserId > 0)
                {
                    var workflows = await _workflowFunctionCallingProvider
                        .GetAvailableAsync(adminUserId, default);
                    result.Workflows = workflows.Select(workflow => new AgentFunctionBindingOptionResponse
                    {
                        Kind = "workflow",
                        Key = workflow.Id.ToString(),
                        WorkflowId = workflow.Id,
                        Name = workflow.Name,
                        Description = workflow.Description,
                        ParameterCount = workflow.Parameters?.Count ?? 0,
                        Available = true
                    }).ToList();
                }

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
                if (agent.IsHuman)
                {
                    response.Success = false;
                    response.ErrorMessage = "Human 是系统保留的特殊参与者，不能停用或启用。";
                    return null;
                }

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

                await PopulateAgentMetadataAsync(result);

                return result;
            });
        }

        [ApiBind]
        public async Task<AppResponseBase<List<KnowledgeBaseOptionResponse>>> GetKnowledgeBaseOptions()
        {
            return await this.GetResponseAsync<List<KnowledgeBaseOptionResponse>>(async (response, logger) =>
            {
                var knowledgeBaseService = base.GetService<KnowledgeBaseService>();
                if (knowledgeBaseService == null)
                {
                    return new List<KnowledgeBaseOptionResponse>();
                }

                var list = await knowledgeBaseService.GetFullListAsync(z => true, z => z.Name, Ncf.Core.Enums.OrderingType.Ascending);
                var embeddingStatuses = await knowledgeBaseService.GetEmbeddingStatusesAsync(list);
                return list.Select(z => new KnowledgeBaseOptionResponse
                {
                    Id = z.Id,
                    Name = z.Name,
                    EmbeddingStatus = embeddingStatuses.TryGetValue(z.Id, out var embeddingStatus)
                        ? embeddingStatus.ToString().ToLowerInvariant()
                        : KnowledgeBaseEmbeddingStatus.Pending.ToString().ToLowerInvariant(),
                    IsEmbedded = KnowledgeBaseService.IsEmbeddingPublished(z)
                }).ToList();
            });
        }

        private async Task ValidateKnowledgeBaseBindingAsync(int? knowledgeBaseId)
        {
            if (!knowledgeBaseId.HasValue)
            {
                return;
            }

            var knowledgeBaseService = base.GetService<KnowledgeBaseService>()
                ?? throw new InvalidOperationException("KnowledgeBase 模块服务未启用，无法绑定知识库。");
            var knowledgeBase = await knowledgeBaseService.GetObjectAsync(z => z.Id == knowledgeBaseId.Value)
                ?? throw new InvalidOperationException($"绑定的知识库不存在：{knowledgeBaseId.Value}");
            if (!KnowledgeBaseService.IsEmbeddingPublished(knowledgeBase))
            {
                var embeddingStatuses = await knowledgeBaseService.GetEmbeddingStatusesAsync([knowledgeBase]);
                if (embeddingStatuses.TryGetValue(knowledgeBase.Id, out var embeddingStatus)
                    && embeddingStatus == KnowledgeBaseEmbeddingStatus.Legacy)
                {
                    throw new InvalidOperationException($"知识库“{knowledgeBase.Name}”已有旧版向量数据，请在知识库中重新向量化并发布后再绑定到 Agent。");
                }

                throw new InvalidOperationException($"知识库“{knowledgeBase.Name}”尚未完成向量化，暂不能绑定到 Agent。");
            }
        }

        private async Task ValidateAgentModelBindingAsync(
            AgentTemplateDto agentTemplateDto,
            bool isHumanParticipant = false)
        {
            if (!Enum.IsDefined(agentTemplateDto.ModelBinding))
            {
                throw new InvalidOperationException("模型绑定方式无效。");
            }

            if (isHumanParticipant)
            {
                if (agentTemplateDto.ModelBinding != AgentModelBindingMode.ManualAiModel)
                {
                    agentTemplateDto.AiModelId = null;
                    return;
                }

                if (agentTemplateDto.AiModelId is not > 0)
                {
                    throw new InvalidOperationException("手动选择 AIModel 时必须选择一个 Chat 类型模型。");
                }

                // Human 本身不会调用模型；仍保存选择，使其在系统 Agent 管理页面中的
                // 任务策略保持可见且可调整，并与其他系统 Agent 使用同一数据契约。
                var humanModelService = base.GetService<AIModelService>()
                    ?? throw new InvalidOperationException("AIKernel 模块服务未启用，无法绑定 AIModel。");
                var humanModel = await humanModelService.GetObjectAsync(z => z.Id == agentTemplateDto.AiModelId.Value)
                    ?? throw new InvalidOperationException($"绑定的 AIModel 不存在：{agentTemplateDto.AiModelId.Value}");
                if (humanModel.ConfigModelType != ConfigModelType.Chat)
                {
                    throw new InvalidOperationException(
                        $"AIModel“{humanModel.Alias}”不是 Chat 类型，不能绑定给 Agent。");
                }
                return;
            }

            var promptCode = string.IsNullOrWhiteSpace(agentTemplateDto.SystemMessage)
                ? agentTemplateDto.PromptCode
                : agentTemplateDto.SystemMessage;
            var isPromptRangeReference = AgentTemplateRunner.IsPromptRangeReference(promptCode);
            if (!isPromptRangeReference
                && agentTemplateDto.ModelBinding != AgentModelBindingMode.ManualAiModel)
            {
                throw new InvalidOperationException(
                    "手动 Prompt 没有 PromptRange 模型可继承，请选择“手动选择 AIModel”。");
            }

            if (agentTemplateDto.ModelBinding != AgentModelBindingMode.ManualAiModel)
            {
                agentTemplateDto.AiModelId = null;
                return;
            }

            if (agentTemplateDto.AiModelId is not > 0)
            {
                throw new InvalidOperationException("手动选择 AIModel 时必须选择一个 Chat 类型模型。");
            }

            var aiModelService = base.GetService<AIModelService>()
                ?? throw new InvalidOperationException("AIKernel 模块服务未启用，无法绑定 AIModel。");
            var aiModel = await aiModelService.GetObjectAsync(z => z.Id == agentTemplateDto.AiModelId.Value)
                ?? throw new InvalidOperationException($"绑定的 AIModel 不存在：{agentTemplateDto.AiModelId.Value}");
            if (aiModel.ConfigModelType != ConfigModelType.Chat)
            {
                throw new InvalidOperationException(
                    $"AIModel“{aiModel.Alias}”不是 Chat 类型，不能绑定给 Agent。");
            }
        }

        public async Task PopulateAgentMetadataAsync<TAgentDto>(IEnumerable<TAgentDto> agentDtos)
            where TAgentDto : AgentTemplateDto
        {
            var dtoList = agentDtos?.Where(z => z != null).ToList() ?? new List<TAgentDto>();
            if (dtoList.Count == 0)
            {
                return;
            }

            var agentIds = dtoList.Select(z => z.Id).Where(z => z > 0).Distinct().ToList();
            var historyService = base.GetRequiredService<ChatGroupHistoryService>();
            var histories = agentIds.Count == 0
                ? new List<Models.DatabaseModel.Models.ChatGroupHistory>()
                : await historyService.GetFullListAsync(z => z.FromAgentTemplateId.HasValue && agentIds.Contains(z.FromAgentTemplateId.Value));

            var historyGroups = histories.GroupBy(z => z.FromAgentTemplateId.Value)
                .ToDictionary(z => z.Key, z => z.ToList());
            foreach (var dto in dtoList)
            {
                dto.FunctionBindings = AgentFunctionBindingCodec.Parse(
                        dto.FunctionCallNames)
                    .Select(CloneBinding)
                    .ToList();
                dto.FunctionCallNames = AgentFunctionBindingCodec.GetLegacyPluginNames(
                    dto.FunctionCallNames);
                if (!historyGroups.TryGetValue(dto.Id, out var agentHistories))
                {
                    continue;
                }

                var completedHistories = agentHistories
                    .Where(z => z.Status == Models.DatabaseModel.Models.Status.Finished)
                    .ToList();
                dto.CompletedConversationRounds = completedHistories.Count;
                dto.CompletedTaskCount = completedHistories.Select(z => z.ChatTaskId).Distinct().Count();
                dto.LastActiveTime = agentHistories.Max(z => z.LastUpdateTime);

                long totalResponseMilliseconds = 0;
                var responseCount = 0;
                foreach (var history in agentHistories)
                {
                    if (!ChatUsageRemarkCodec.TryDecodeMessage(history.AdminRemark, out var usage))
                    {
                        continue;
                    }

                    dto.PromptTokens += usage.PromptTokens;
                    dto.CompletionTokens += usage.CompletionTokens;
                    dto.TotalTokens += usage.TotalTokens;
                    totalResponseMilliseconds += usage.ResponseMilliseconds;
                    responseCount++;
                }

                dto.AverageResponseMilliseconds = responseCount == 0
                    ? 0
                    : Math.Round((double)totalResponseMilliseconds / responseCount, 2);
            }

            var knowledgeBaseIds = dtoList.Where(z => z.KnowledgeBaseId.HasValue)
                .Select(z => z.KnowledgeBaseId.Value)
                .Distinct()
                .ToList();
            var knowledgeBaseService = base.GetService<KnowledgeBaseService>();
            if (knowledgeBaseService == null || knowledgeBaseIds.Count == 0)
            {
                return;
            }

            var knowledgeBases = await knowledgeBaseService.GetFullListAsync(z => knowledgeBaseIds.Contains(z.Id));
            var knowledgeBaseNames = knowledgeBases.ToDictionary(z => z.Id, z => z.Name);
            foreach (var dto in dtoList.Where(z => z.KnowledgeBaseId.HasValue))
            {
                if (knowledgeBaseNames.TryGetValue(dto.KnowledgeBaseId.Value, out var name))
                {
                    dto.KnowledgeBaseName = name;
                }
                else
                {
                    dto.KnowledgeBaseName = $"知识库不可用（ID: {dto.KnowledgeBaseId.Value}）";
                }
            }
        }

        private int GetCurrentAdminUserId()
        {
            try
            {
                return base.GetService<IAdminWorkContextProvider>()?.GetAdminWorkContext()?.AdminUserId ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private static AgentFunctionBindingDto CloneBinding(AgentFunctionBindingDto binding)
            => binding == null
                ? null
                : new AgentFunctionBindingDto
                {
                    Kind = binding.Kind,
                    Key = binding.Key,
                    Name = binding.Name,
                    Description = binding.Description,
                    ModuleUid = binding.ModuleUid,
                    FunctionKey = binding.FunctionKey,
                    WorkflowId = binding.WorkflowId
                };

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
                    //var clientTransport = new SseClientTransport(new SseClientTransportOptions()
                    //{
                    //    Endpoint = new Uri(endpointUrl),
                    //    Name = endpointName
                    //});

                    //await using var client = await McpClientFactory.CreateAsync(clientTransport);
                    //var tools = await client.ListToolsAsync();


                    var testServerTool = new HostedMcpServerTool(endpointName, new Uri(endpointUrl))
                    {
                        ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire
                    };

                    var tools = new List<AITool> { testServerTool };

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

                    //await clientTransport.DisposeAsync();

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
            var publishedA2AAgentService = base.GetRequiredService<PublishedA2AAgentService>();

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

                if (agent.IsHuman)
                {
                    blocked++;
                    logger.Append($"✗ 阻止删除 Agent【{agent.Name}】：Human 是系统保留的特殊参与者");
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

                // 发布配置是 Agent 的附加能力；删除本地 Agent 时一并撤销，避免遗留不可访问的公开标识。
                var publishedA2AAgent = await publishedA2AAgentService.GetByAgentTemplateIdAsync(id);
                if (publishedA2AAgent != null)
                {
                    await publishedA2AAgentService.DeleteObjectAsync(publishedA2AAgent);
                }

                await _agentsTemplateService.DeleteObjectAsync(agent);
                deleted++;
                logger.Append($"✓ 已删除 Agent【{agent.Name}】（成员关系 {members.Count} 条，消息记录 {histories.Count} 条{(publishedA2AAgent != null ? "，已撤销 A2A 发布" : string.Empty)}）");
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
