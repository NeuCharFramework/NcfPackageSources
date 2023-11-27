using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.HuggingFace.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Newtonsoft.Json;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Kernel.Helpers;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.AppService;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptResultService : ServiceBase<PromptResult>
    {
        public PromptResultService(IRepositoryBase<PromptResult> repo, IServiceProvider serviceProvider,
            LlmModelService llmModelService) : base(repo,
            serviceProvider)
        {
            _llmModelService = llmModelService;
        }

        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        // private readonly PromptItemService _promptItemService;
        private readonly LlmModelService _llmModelService;


        public async Task<List<PromptResult>> BatchGenerateResultAsync(PromptItem promptItem, int count)
        {
            List<PromptResult> list = new List<PromptResult>();
            for (var i = 0; i < count; i++)
            {
                var promptResult = await this.GenerateResultAsync(promptItem);
                list.Add(promptResult);
            }

            return list;
        }

        /// <summary>
        /// 传入promptItem，生成结果
        /// 暂时只能在PromptItemAppService中调用
        /// </summary>
        /// <param name="promptItem"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<PromptResult> GenerateResultAsync(PromptItem promptItem)
        {
            // 从数据库中获取模型信息
            var model = await _llmModelService.GetObjectAsync(z => z.Id == promptItem.ModelId);
            if (model == null)
            {
                throw new NcfExceptionBase($"未找到模型{promptItem.ModelId}");
            }

            var userId = "Test";

            var aiSettings = this.BuildSenparcAiSetting(model);
            // //创建 AI Handler 处理器（也可以通过工厂依赖注入）
            // var handler = new SemanticAiHandler(new SemanticKernelHelper(aiSettings));
            //
            // //定义 AI 接口调用参数和 Token 限制等
            // var promptParameter = new PromptConfigParameter()
            // {
            //     MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 2000,
            //     Temperature = promptItem.Temperature,
            //     TopP = promptItem.TopP,
            //     FrequencyPenalty = promptItem.FrequencyPenalty,
            //     PresencePenalty = promptItem.PresencePenalty
            // };

            // 需要在变量前添加$
            const string functionPrompt = "请根据提示输出对应内容：\n{{$input}}";

            var dt1 = SystemTime.Now;
            var resp = model.ModelType switch
            {
                Constants.OpenAI => await WithOpenAIChatCompletionService(promptItem, model),
                Constants.AzureOpenAI => await WithAzureOpenAIChatCompletionService(promptItem, model),
                Constants.HuggingFace => await WithHuggingFaceCompletionService(promptItem, model),
                _ => throw new NotImplementedException()
            };

            // var skContext = iWantToRun.CreateNewContext().context;
            // // var context = iWantToRun.CreateNewContext();
            // skContext.Variables["input"] = promptItem.Content;
            //
            // var aiRequest = iWantToRun.CreateRequest(skContext.Variables, true);
            // var result = await iWantToRun.RunAsync(aiRequest);

            // todo 计算token消耗
            // 简单计算
            // num_prompt_tokens = len(encoding.encode(string))
            // gap_between_send_receive = 15 * len(kwargs["messages"])
            // num_prompt_tokens += gap_between_send_receive
            var promptCostToken = 0;
            var resultCostToken = 0;

            var promptResult = new PromptResult(
                promptItem.ModelId, resp, SystemTime.DiffTotalMS(dt1),
                0, 0, null, false, TestType.Text,
                promptCostToken, resultCostToken, promptCostToken + resultCostToken,
                promptItem.Version, promptItem.Id);

            await base.SaveObjectAsync(promptResult);

            return promptResult;
        }

        private static async Task<string> WithOpenAIChatCompletionService(PromptItem promptItem, LlmModel model)
        {
            OpenAIChatCompletion chatGPT = new(
                modelId: model.GetModelId(),
                apiKey: model.ApiKey,
                organization: model.OrganizationId
            );
            // add system prompt
            var chatHistory = chatGPT.CreateNewChat(promptItem.ChatSystemPrompt);
            // add prompt
            chatHistory.AddUserMessage(promptItem.Content);
            string reply = await chatGPT.GenerateMessageAsync(chatHistory, BuildAIRequestSettings(promptItem));
            // chatHistory.AddAssistantMessage(reply);
            // return chatHistory.Last().Content;
            return reply;
        }

        private static async Task<string> WithAzureOpenAIChatCompletionService(PromptItem promptItem, LlmModel model)
        {
            // 不在意apiVersion， why?
             var chatGPT = new AzureOpenAIChatCompletion(
                endpoint: model.Endpoint,
                apiKey: model.ApiKey,
                deploymentName: model.GetModelId()
            );
            // add system prompt
            var chatHistory = chatGPT.CreateNewChat();
            // chatGPT.CreateNewChat(promptItem.ChatSystemPrompt ?? "请根据提示输出对应内容：\n{{$input}}");

            // add prompt
            chatHistory.AddUserMessage(promptItem.Content);

            // 调用模型
            var resultList = await chatGPT
                .GetChatCompletionsAsync(chatHistory, BuildAIRequestSettings(promptItem)).ConfigureAwait(true);
            var firstChatMessage = await resultList[0].GetChatMessageAsync().ConfigureAwait(true);
            // chatHistory.AddAssistantMessage(reply);
            // return chatHistory.Last().Content;
            return firstChatMessage.Content;
        }

        /// <summary>
        /// 先用sk的原生Connector
        /// 调用hf模型,
        /// **模型接口需要遵循SK的规范**
        /// </summary>
        /// <param name="promptItem"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private static async Task<string> WithHuggingFaceCompletionService(PromptItem promptItem, LlmModel model)
        {
            var conn = new HuggingFaceTextCompletion(model.GetModelId(), endpoint: model.Endpoint);
            // var aiRequestSettings = BuildAIRequestSettings(promptItem);
            return await conn.CompleteAsync(promptItem.Content, BuildAIRequestSettings(promptItem));
        }

        private static OpenAIRequestSettings BuildAIRequestSettings(PromptItem promptItem)
        {
            var aiSettings = new OpenAIRequestSettings()
            {
                Temperature = promptItem.Temperature,
                TopP = promptItem.TopP,
                MaxTokens = promptItem.MaxToken,
                FrequencyPenalty = promptItem.FrequencyPenalty,
                PresencePenalty = promptItem.PresencePenalty,
            };
            if (!string.IsNullOrWhiteSpace(promptItem.StopSequences))
            {
                aiSettings.StopSequences = promptItem.StopSequences.Split(",");
            }

            return aiSettings;
        }


        public async Task<PromptResult> Score(int id, int score)
        {
            // 根据id搜索数据库
            // var promptResult = _promptResultRepository.GetObjectById(id);
            var promptResult = await base.GetObjectAsync(z => z.Id == id);
            if (promptResult == null)
            {
                throw new NcfExceptionBase($"未找到{id}对应的结果");
            }

            promptResult.Scoring(score);
            await base.SaveObjectAsync(promptResult);

            return promptResult;
        }


        private SenparcAiSetting BuildSenparcAiSetting(LlmModel llmModel)
        {
            var aiSettings = new SenparcAiSetting();

            if (!Enum.TryParse(llmModel.ModelType, out AiPlatform aiPlatform))
                throw new Exception("无法转换为AiPlatform");
            aiSettings.AiPlatform = aiPlatform;
            switch (aiPlatform)
            {
                case AiPlatform.AzureOpenAI:
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llmModel.ApiKey,
                        AzureOpenAIApiVersion = llmModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = llmModel.Endpoint
                    };
                    break;
                case AiPlatform.HuggingFace:
                    aiSettings.HuggingFaceKeys = new HuggingFaceKeys()
                    {
                        Endpoint = llmModel.Endpoint
                    };
                    break;
                case AiPlatform.OpenAI:
                    aiSettings.OpenAIKeys = new OpenAIKeys()
                    {
                        ApiKey = llmModel.ApiKey,
                        OrganizationId = llmModel.OrganizationId
                    };
                    break;
                default:
                    break;
            }


            return aiSettings;
        }

        public async Task RobotScore(List<int> promptResultListToEval)
        {
            foreach (var id in promptResultListToEval)
            {
                // todo 根据id获取PromptResult
                // 然后调用模型对其进行评分
            }
        }
    }
}