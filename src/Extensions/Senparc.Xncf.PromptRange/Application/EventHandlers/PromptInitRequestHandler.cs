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
            _logger.LogInformation($"Received Prompt Init Request: {@event.RequestId}");

            try 
            {
                // 1. Check if PromptRange "PromptCatalyzer" exists
                var range = await _promptRangeService.GetObjectAsync(z => z.Alias == "PromptCatalyzer");
                if (range == null)
                {
                    var rangeDto = await _promptRangeService.AddAsync("PromptCatalyzer");
                    range = await _promptRangeService.GetObjectAsync(z => z.Id == rangeDto.Id);
                }

                // 2. Check if PromptItem exists in this range (T1-A1)
                // Assuming we just want *any* valid item, or specifically the root one.
                // We'll search for the first item.
                var item = await _promptItemService.GetObjectAsync(z => z.RangeId == range.Id);

                if (item == null)
                {
                    // Create default PromptItem
                    var model = await _aiModelService.GetObjectAsync(z => true);
                    if (model == null)
                    {
                        throw new Exception("No AI Model found in AIKernel. Please configure a model first.");
                    }

                    var request = new PromptItem_AddRequest
                    {
                        RangeId = range.Id,
                        ModelId = model.Id,
                        Content = "You are an expert Prompt Engineer (PromptCatalyzer). Your goal is to optimize the user's prompt based on their requirements.",
                        IsTopTactic = true,
                        NumsOfResults = 0,
                        MaxToken = 4000,
                        Temperature = 0.7f,
                        TopP = 0.9f,
                        FrequencyPenalty = 0,
                        PresencePenalty = 0,
                        StopSequences = null,
                        IsDraft = true // Start as draft to avoid auto-triggering execution
                    };

                    var stringRequest = System.Text.Json.JsonSerializer.Serialize(request);
                    _logger.LogInformation($"Creating default prompt item with request: {stringRequest}");

                    // PromptItemService.AddPromptItemAsync logic is complex, requires careful handling
                    // Here we rely on the service to handle creation
                    await _promptItemService.AddPromptItemAsync(request);
                    
                    // Fetch again to get the generated item
                    item = await _promptItemService.GetObjectAsync(z => z.RangeId == range.Id);
                }
                
                if (item == null)
                {
                     throw new Exception("Failed to create PromptItem.");
                }

                bool success = true;
                string message = "Initialized successfully.";
                // Construct the prompt code needed by AgentsManager (e.g., "RANGE_NAME-T1-A1")
                // FullVersion is typically stored in DB or constructed
                string promptCode = item.FullVersion; 

                var response = new PromptInitResponseEvent(@event.RequestId, promptCode, success, message);
                await _eventBus.PublishAsync(response);
                
                _logger.LogInformation($"Available Prompt Code: {promptCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PromptInitRequest");
                var response = new PromptInitResponseEvent(@event.RequestId, null, false, ex.Message);
                await _eventBus.PublishAsync(response);
            }
        }
    }
}
