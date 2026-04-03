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

namespace Senparc.Xncf.AgentsManager.OHS.Remote.Controllers
{
    /// <summary>
    ///PromptCatalyzer initializes API Controller
    /// </summary>
    [ApiController]
    [Route("api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService")]
    public class PromptCatalyzerInitController : ControllerBase
    {
        private readonly PromptOptimizationService _promptOptimizationService;
        private readonly AgentsTemplateService _agentsTemplateService;
        private readonly AIModelService _aiModelService;
        private readonly ILogger<PromptCatalyzerInitController> _logger;

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
        /// Check if PromptCatalyzer has been initialized
        /// </summary>
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
        /// Get all available Chat type AI Models
        /// </summary>
        [HttpGet("GetAvailableModels")]
        public IActionResult GetAvailableModels()
        {
            try
            {
                // First obtain all models for debugging
                var allModels = _aiModelService.GetFullList(z => true, z => z.Id, Ncf.Core.Enums.OrderingType.Ascending);
                _logger.LogInformation($"数据库中共有 {allModels.Count} 个 AI Model");
                
                // Record the details of each model
                foreach (var model in allModels)
                {
                    _logger.LogInformation($"Model ID: {model.Id}, Alias: {model.Alias}, ConfigModelType: {model.ConfigModelType}, Show: {model.Show}");
                }
                
                // Query the model of Chat type (only the type is checked, the Show field is not checked)
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
        /// Initialize PromptCatalyzer Agent and related Prompt resources
        /// </summary>
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

                // Verify that Model exists
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

                // Call initialization service
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
    ///PromptCatalyzer Status DTO
    /// </summary>
    public class PromptCatalyzerStatusDto
    {
        public bool IsInitialized { get; set; }
        public int? AgentId { get; set; }
        public string PromptCode { get; set; }
    }

    /// <summary>
    /// List of available models DTO
    /// </summary>
    public class AvailableModelsDto
    {
        public List<AIModelInfoDto> Models { get; set; }
        public int? RecommendedModelId { get; set; }
    }

    /// <summary>
    ///AI Model Information DTO
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
    ///Initialization request DTO
    /// </summary>
    public class InitializeRequestDto
    {
        public int? ModelId { get; set; }
    }

    /// <summary>
    ///Initialize response DTO
    /// </summary>
    public class InitializeResponseDto
    {
        public string PromptCode { get; set; }
        public string Message { get; set; }
    }

    #endregion
}
