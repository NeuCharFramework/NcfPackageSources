using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Senparc.Xncf.PromptRange.Application.EventHandlers
{
    public class PromptInitRequestHandler : IIntegrationEventHandler<PromptInitRequestEvent>
    {
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly AIModelService _aiModelService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<PromptInitRequestHandler> _logger;

        public PromptInitRequestHandler(
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            AIModelService aiModelService,
            IEventBus eventBus,
            ILogger<PromptInitRequestHandler> logger)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _aiModelService = aiModelService;
            _eventBus = eventBus;
            _logger = logger;
        }

        public async Task Handle(PromptInitRequestEvent @event, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received Prompt Init Request: RequestId={RequestId}, ModelId={ModelId}, Depth={Depth}, Chain={Chain}", 
                @event.RequestId, @event.ModelId, @event.Depth, @event.EventChain);

            try 
            {
                // 1. Check if PromptRange "PromptCatalyzer" exists
                var range = await _promptRangeService.GetObjectAsync(z => z.Alias == "PromptCatalyzer");
                if (range == null)
                {
                    _logger.LogInformation("Creating PromptCatalyzer Range...");
                    var rangeDto = await _promptRangeService.AddAsync("PromptCatalyzer");
                    range = await _promptRangeService.GetObjectAsync(z => z.Id == rangeDto.Id);
                }

                // 2. Determine which Model to use
                int modelId;
                if (@event.ModelId.HasValue)
                {
                    // User specified a Model
                    modelId = @event.ModelId.Value;
                    _logger.LogInformation("Using user-specified Model ID: {ModelId}", modelId);
                    
                    // Verify the model exists
                    var specifiedModel = await _aiModelService.GetObjectAsync(z => z.Id == modelId);
                    if (specifiedModel == null)
                    {
                        throw new Exception($"Specified AI Model ID {modelId} not found");
                    }
                }
                else
                {
                    // Use the first available Chat model
                    var model = await _aiModelService.GetObjectAsync(
                        z => z.Show == true && 
                             z.ConfigModelType == Senparc.Xncf.AIKernel.Domain.Models.ConfigModelType.Chat);
                    
                    if (model == null)
                    {
                        throw new Exception("No AI Model found in AIKernel. Please configure a Chat model first.");
                    }
                    
                    modelId = model.Id;
                    _logger.LogInformation("Using default Model: {Alias} (ID: {ModelId})", model.Alias, modelId);
                }

                // 3. Check if PromptItem exists in this range
                var item = await _promptItemService.GetObjectAsync(z => z.RangeId == range.Id);

                if (item == null)
                {
                    _logger.LogInformation("Creating default PromptItem with Model ID: {ModelId}...", modelId);
                    
                    // Create default PromptItem
                    var request = new PromptItem_AddRequest
                    {
                        RangeId = range.Id,
                        ModelId = modelId,  // Use selected or default model
                        Content = @"You are an expert Prompt Engineer (PromptCatalyzer). 

Your goal is to optimize the user's prompt and parameters to achieve better results.

Guidelines for optimization:
1. Analyze the user's current prompt and identify areas for improvement
2. Make the prompt more clear, specific, and effective
3. Adjust parameters (Temperature, TopP, etc.) based on the use case:
   - Lower Temperature (0.3-0.5) for factual, consistent outputs
   - Higher Temperature (0.7-0.9) for creative, diverse outputs
4. Provide reasoning for your optimization decisions

Always respond in JSON format with optimized content and parameters.",
                        IsTopTactic = true,
                        NumsOfResults = 0,
                        MaxToken = 4000,
                        Temperature = 0.7f,
                        TopP = 0.9f,
                        FrequencyPenalty = 0,
                        PresencePenalty = 0,
                        StopSequences = null,
                        IsDraft = true, // Start as draft
                        Note = $"Auto-created for PromptCatalyzer initialization with Model ID: {modelId}"
                    };

                    var stringRequest = System.Text.Json.JsonSerializer.Serialize(request);
                    _logger.LogInformation("Creating default prompt item with request: {Request}", stringRequest);

                    await _promptItemService.AddPromptItemAsync(request);
                    
                    // Fetch again to get the generated item
                    item = await _promptItemService.GetObjectAsync(z => z.RangeId == range.Id);
                }
                
                if (item == null)
                {
                     throw new Exception("Failed to create PromptItem.");
                }

                bool success = true;
                string message = $"Initialized successfully with Model ID: {modelId}";
                string promptCode = item.FullVersion; 

                var response = new PromptInitResponseEvent(@event.RequestId, promptCode, success, message);
                
                // 使用 PublishDerivedAsync 继承事件链信息（防止循环引用）
                await _eventBus.PublishDerivedAsync(response, @event);
                
                _logger.LogInformation("Available Prompt Code: {PromptCode}, Model ID: {ModelId}", 
                    promptCode, modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PromptInitRequest");
                var response = new PromptInitResponseEvent(@event.RequestId, null, false, ex.Message);
                
                // 即使是错误响应，也需要继承事件链
                await _eventBus.PublishDerivedAsync(response, @event);
            }
        }
    }
}
