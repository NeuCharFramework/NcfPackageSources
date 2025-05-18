using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Senparc.CO2NET;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AgentsManager.Domain.Services;
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
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                SenparcAI_GetByVersionResponse promptResult;
                var promptCode = request.GetySystemMessagePromptCode();

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
                    Enum.Parse<HookRobotType>(request.HookRobotType.SelectedValues.FirstOrDefault()), request.HookRobotParameter, request.FunctionCallNames);

                await this._agentsTemplateService.UpdateAgentTemplateAsync(request.Id, agentTemplateDto);

                logger.Append("Agent 模板更新成功！");
                logger.Append("当前代理使用的 Prompt 模板：" + promptTemplate);

                return logger.ToString();
            });
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

                var listDto = new PagedList<AgentTemplateSimpleStatusDto>(list
                    .Select(z =>
                    _agentsTemplateService.Mapping<AgentTemplateSimpleStatusDto>(z)).ToList(),
                        list.PageIndex, list.PageCount, list.TotalCount, list.SkipCount);

                var result = new AgentTemplate_GetListResponse()
                {
                    List = listDto
                };
                return result;
            });
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

                    var client = await McpClientFactory.CreateAsync(clientTransport);
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
