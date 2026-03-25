using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// PromptCatalyzer 初始化 AppService
    /// 提供检查状态、获取可用模型、初始化等功能
    /// </summary>
    public class PromptCatalyzerInitAppService : AppServiceBase
    {
        private readonly PromptOptimizationService _promptOptimizationService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly AIModelService _aiModelService;
        private readonly ILogger<PromptCatalyzerInitAppService> _logger;

        public PromptCatalyzerInitAppService(
            IServiceProvider serviceProvider,
            PromptOptimizationService promptOptimizationService,
            AgentsTemplateService agentsTemplateService,
            AIModelService aiModelService,
            ILogger<PromptCatalyzerInitAppService> logger)
            : base(serviceProvider)
        {
            _promptOptimizationService = promptOptimizationService;
            _agentsTemplateService = agentsTemplateService;
            _aiModelService = aiModelService;
            _logger = logger;
        }

        /// <summary>
        /// 检查 PromptCatalyzer 是否已初始化
        /// </summary>
        [HttpGet]
        public async Task<AppResponseBase<PromptCatalyzerStatusDto>> CheckStatus()
        {
            return await this.GetResponseAsync<PromptCatalyzerStatusDto>(
                (response, logger) =>
                {
                    var agent = _agentsTemplateService.GetObject(z => z.Name == "PromptCatalyzer");
                    
                    return Task.FromResult(new PromptCatalyzerStatusDto
                    {
                        IsInitialized = agent != null,
                        AgentId = agent?.Id,
                        PromptCode = agent?.SystemMessage
                    });
                }
            );
        }

        /// <summary>
        /// 获取所有可用的 Chat 类型 AI Model
        /// </summary>
        [HttpGet]
        public async Task<AppResponseBase<AvailableModelsDto>> GetAvailableModels()
        {
            return await this.GetResponseAsync<AvailableModelsDto>(
                (response, logger) =>
                {
                    var chatModels = _aiModelService
                        .GetFullList(z => z.Show == true && 
                                         z.ConfigModelType == AIKernel.Domain.Models.ConfigModelType.Chat,
                                    z => z.Id, Ncf.Core.Enums.OrderingType.Ascending)
                        .Select(m => new AIModelInfoDto
                        {
                            Id = m.Id,
                            Alias = m.Alias,
                            DeploymentName = m.DeploymentName,
                            AiPlatform = m.AiPlatform.ToString(),
                            Note = m.Note,
                            ConfigModelType = m.ConfigModelType.ToString()
                        })
                        .ToList();

                    if (!chatModels.Any())
                    {
                        throw new Exception("未找到可用的 Chat 类型 AI Model。请先在 AIKernel 模块中配置至少一个 Chat Model。");
                    }

                    return Task.FromResult(new AvailableModelsDto
                    {
                        Models = chatModels,
                        RecommendedModelId = chatModels.FirstOrDefault()?.Id
                    });
                }
            );
        }

        /// <summary>
        /// 初始化 PromptCatalyzer（创建 Prompt、Agent 等资源）
        /// </summary>
        [HttpPost]
        public async Task<AppResponseBase<PromptInitResponseEvent>> Initialize([FromBody] InitializeRequestDto request)
        {
            return await this.GetResponseAsync<PromptInitResponseEvent>(
                async (response, logger) =>
                {
                    _logger.LogInformation("Initializing PromptCatalyzer with ModelId: {ModelId}", request.ModelId);

                    if (!request.ModelId.HasValue || request.ModelId.Value <= 0)
                    {
                        throw new Exception("请选择一个有效的 AI Model");
                    }

                    // 验证 Model 是否存在
                    var model = _aiModelService.GetObject(z => z.Id == request.ModelId.Value);
                    if (model == null)
                    {
                        throw new Exception($"未找到 ID 为 {request.ModelId.Value} 的 AI Model");
                    }

                    if (model.ConfigModelType != AIKernel.Domain.Models.ConfigModelType.Chat)
                    {
                        throw new Exception($"Model '{model.Alias}' 不是 Chat 类型，请选择 Chat 类型的 Model");
                    }

                    // 调用初始化服务
                    var result = await _promptOptimizationService.EnsureInitializedAsync(request.ModelId);

                    return result;
                }
            );
        }
    }

    #region DTOs

    /// <summary>
    /// PromptCatalyzer 状态 DTO
    /// </summary>
    public class PromptCatalyzerStatusDto
    {
        public bool IsInitialized { get; set; }
        public int? AgentId { get; set; }
        public string PromptCode { get; set; }
    }

    /// <summary>
    /// 可用模型列表 DTO
    /// </summary>
    public class AvailableModelsDto
    {
        public List<AIModelInfoDto> Models { get; set; }
        public int? RecommendedModelId { get; set; }
    }

    /// <summary>
    /// AI Model 信息 DTO
    /// </summary>
    public class AIModelInfoDto
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public string DeploymentName { get; set; }
        public string AiPlatform { get; set; }
        public string Note { get; set; }
        public string ConfigModelType { get; set; }
    }

    /// <summary>
    /// 初始化请求 DTO
    /// </summary>
    public class InitializeRequestDto
    {
        public int? ModelId { get; set; }
    }

    #endregion
}
