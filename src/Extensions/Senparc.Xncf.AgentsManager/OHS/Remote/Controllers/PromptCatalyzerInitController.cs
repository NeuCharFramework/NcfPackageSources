/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptCatalyzerInitController.cs
    File description: PromptCatalyzerInitController related implementation

    Created by: Senparc - 20260324

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260729
    Change description: v0.12.1-preview7 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.AgentsManager.Domain.Services;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;

namespace Senparc.Xncf.AgentsManager.OHS.Remote.Controllers
{
    /// <summary>
    /// PromptCatalyzer initialize API Controller
    /// </summary>
    [ApiController]
    [Route("api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService")]
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class PromptCatalyzerInitController : ControllerBase
    {
        private readonly PromptOptimizationService _promptOptimizationService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly AIModelService _aiModelService;
        private readonly ILogger<PromptCatalyzerInitController> _logger;

        /// <summary>
        /// Initializes the PromptCatalyzer status and initialization controller.
        /// </summary>
        /// <param name="promptOptimizationService">The initialization/optimization service.</param>
        /// <param name="agentsTemplateService">The AgentTemplate persistence service.</param>
        /// <param name="aiModelService">The AI model persistence service.</param>
        /// <param name="logger">The controller logger.</param>
        public PromptCatalyzerInitController(
            PromptOptimizationService promptOptimizationService,
            AgentsTemplateService agentsTemplateService,
            AIModelService aiModelService,
            ILogger<PromptCatalyzerInitController> logger)
        {
            _promptOptimizationService = promptOptimizationService;
            _agentsTemplateService = agentsTemplateService;
            _aiModelService = aiModelService;
            _logger = logger;
        }

        /// <summary>
        /// Returns whether the PromptCatalyzer AgentTemplate has been initialized.
        /// </summary>
        /// <returns>An HTTP 200 response containing initialization state or a diagnostic failure.</returns>
        [HttpGet("CheckStatus")]
        public IActionResult CheckStatus()
        {
            try
            {
                var agent = _agentsTemplateService.GetObject(z => z.Name == "PromptCatalyzer");
                
                var result = new AppResponseBase<PromptCatalyzerStatusDto>
                {
                    Success = true,
                    Data = new PromptCatalyzerStatusDto
                    {
                        IsInitialized = agent != null,
                        AgentId = agent?.Id,
                        PromptCode = agent?.SystemMessage
                    }
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查 PromptCatalyzer 状态失败");
                return Ok(new AppResponseBase<PromptCatalyzerStatusDto>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        /// <summary>
        /// Lists configured Chat-capable AI models that may initialize PromptCatalyzer.
        /// </summary>
        /// <returns>An HTTP 200 response containing available model metadata.</returns>
        [HttpGet("GetAvailableModels")]
        public IActionResult GetAvailableModels()
        {
            try
            {
                //Legacy comment translated to English.
                var allModels = _aiModelService.GetFullList(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);
                _logger.LogInformation($"数据库中共有 {allModels.Count} 个 AI Model");
                
                //Legacy comment translated to English.
                foreach (var model in allModels)
                {
                    _logger.LogInformation($"Model ID: {model.Id}, Alias: {model.Alias}, ConfigModelType: {model.ConfigModelType}, Show: {model.Show}");
                }
                
                //Legacy comment translated to English.
                var chatModels = _aiModelService
                    .GetFullList(z => z.ConfigModelType == AIKernel.Domain.Models.ConfigModelType.Chat,
                                z => z.Id, Ncf.Core.Enums.OrderingType.Ascending)
                    .Select(m => new AIModelInfoDto
                    {
                        Id = m.Id,
                        Alias = m.Alias,
                        DeploymentName = m.DeploymentName,
                        AiPlatform = m.AiPlatform.ToString(),
                        Note = m.Note,
                        ConfigModelType = m.ConfigModelType.ToString(),
                        Show = m.Show
                    })
                    .ToList();

                _logger.LogInformation($"找到 {chatModels.Count} 个 Chat 类型的 AI Model");

                if (!chatModels.Any())
                {
                    _logger.LogWarning("未找到任何 Chat 类型的 AI Model");
                    return Ok(new AppResponseBase<AvailableModelsDto>
                    {
                        Success = false,
                        ErrorMessage = $"未找到可用的 Chat 类型 AI Model。数据库中共有 {allModels.Count} 个模型，但没有 Chat 类型的。请先在 AIKernel 模块中配置至少一个 Chat Model。"
                    });
                }

                var result = new AppResponseBase<AvailableModelsDto>
                {
                    Success = true,
                    Data = new AvailableModelsDto
                    {
                        Models = chatModels,
                        RecommendedModelId = chatModels.FirstOrDefault()?.Id
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用 AI Model 列表失败");
                return Ok(new AppResponseBase<AvailableModelsDto>
                {
                    Success = false,
                    ErrorMessage = $"获取失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Initializes PromptCatalyzer against a selected Chat AI model.
        /// </summary>
        /// <param name="request">The model identifier request.</param>
        /// <returns>An HTTP 200 response containing the initialization result.</returns>
        [HttpPost("Initialize")]
        public async Task<IActionResult> Initialize([FromBody] InitializeRequestDto request)
        {
            try
            {
                _logger.LogInformation("Initializing PromptCatalyzer with ModelId: {ModelId}", request.ModelId);

                if (!request.ModelId.HasValue || request.ModelId.Value <= 0)
                {
                    return Ok(new AppResponseBase<InitializeResponseDto>
                    {
                        Success = false,
                        ErrorMessage = "请选择一个有效的 AI Model"
                    });
                }

                //Legacy comment translated to English.
                var model = _aiModelService.GetObject(z => z.Id == request.ModelId.Value);
                if (model == null)
                {
                    return Ok(new AppResponseBase<InitializeResponseDto>
                    {
                        Success = false,
                        ErrorMessage = $"未找到 ID 为 {request.ModelId.Value} 的 AI Model"
                    });
                }

                if (model.ConfigModelType != AIKernel.Domain.Models.ConfigModelType.Chat)
                {
                    return Ok(new AppResponseBase<InitializeResponseDto>
                    {
                        Success = false,
                        ErrorMessage = $"Model '{model.Alias}' 不是 Chat 类型，请选择 Chat 类型的 Model"
                    });
                }

                //Legacy comment translated to English.
                var initResult = await _promptOptimizationService.EnsureInitializedAsync(request.ModelId.Value);

                var result = new AppResponseBase<InitializeResponseDto>
                {
                    Success = initResult.Success,
                    ErrorMessage = initResult.ErrorMessage,
                    Data = new InitializeResponseDto
                    {
                        PromptCode = initResult.PromptCode,
                        Message = initResult.Success ? "初始化成功" : initResult.ErrorMessage
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化 PromptCatalyzer 失败");
                return Ok(new AppResponseBase<InitializeResponseDto>
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }

    #region DTOs

    /// <summary>
    /// PromptCatalyzer status DTO
    /// </summary>
    public class PromptCatalyzerStatusDto
    {
        public bool IsInitialized { get; set; }
        public int? AgentId { get; set; }
        public string PromptCode { get; set; }
    }

    /// <summary>
    /// Describes the Chat models available for PromptCatalyzer initialization.
    /// </summary>
    public class AvailableModelsDto
    {
        public List<AIModelInfoDto> Models { get; set; }
        public int? RecommendedModelId { get; set; }
    }

    /// <summary>
    /// AI Model information DTO
    /// </summary>
    public class AIModelInfoDto
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public string DeploymentName { get; set; }
        public string AiPlatform { get; set; }
        public string Note { get; set; }
        public string ConfigModelType { get; set; }
        public bool Show { get; set; }
    }

    /// <summary>
    /// initializerequest DTO
    /// </summary>
    public class InitializeRequestDto
    {
        public int? ModelId { get; set; }
    }

    /// <summary>
    /// initializeresponse DTO
    /// </summary>
    public class InitializeResponseDto
    {
        public string PromptCode { get; set; }
        public string Message { get; set; }
    }

    #endregion
}
