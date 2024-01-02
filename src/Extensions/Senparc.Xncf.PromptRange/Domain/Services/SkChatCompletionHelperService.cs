using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextGeneration;
using Microsoft.SemanticKernel.Connectors.AI.HuggingFace.TextGeneration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Senparc.Xncf.PromptRange.Models;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public static class SkChatCompletionHelperService
    {

        public static async Task<string> WithOpenAIChatCompletionService(PromptItem promptItem, LlModel model)
        {
            OpenAIChatCompletionService chatGPT = new(
                modelId: model.GetModelId(),
                apiKey: model.ApiKey,
                organization: model.OrganizationId
            );
            // add system prompt
            ChatHistory chatHistory = new ChatHistory(promptItem.ChatSystemPrompt);
            // add prompt
            chatHistory.AddUserMessage(promptItem.Content);
            string reply = await chatGPT.GetChatMessageContentAsync(chatHistory, BuildAIRequestSettings(promptItem));
            // chatHistory.AddAssistantMessage(reply);
            // return chatHistory.Last().Content;
            return reply;
        }

        public static async Task<string> WithAzureOpenAIChatCompletionService(PromptItem promptItem, LlModel model)
        {
            // 不在意apiVersion， why?
            var chatGPT = new AzureOpenAIChatCompletionService(
                modelId: model.GetModelId(),
                endpoint: model.Endpoint,
                apiKey: model.ApiKey,
                deploymentName: model.GetModelId()
            );
            // add system prompt
            var chatHistory = new ChatHistory(); //chatGPT.CreateNewChat(); TODO:没有找到替代方法
            // chatGPT.CreateNewChat(promptItem.ChatSystemPrompt ?? "请根据提示输出对应内容：\n{{$input}}");

            // add prompt
            chatHistory.AddUserMessage(promptItem.Content);

            // 调用模型
            var resultList = await chatGPT.GetChatMessageContentsAsync(chatHistory, BuildAIRequestSettings(promptItem)).ConfigureAwait(true);
            var firstChatMessage = resultList[0];
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
        public static async Task<string> WithHuggingFaceCompletionService(PromptItem promptItem, LlModel model)
        {
#pragma warning disable SKEXP0020
            var conn = new HuggingFaceTextGenerationService(model.GetModelId(), endpoint: model.Endpoint);
            // var aiRequestSettings = BuildAIRequestSettings(promptItem);

            var result= await conn.GetTextContentsAsync(promptItem.Content, BuildAIRequestSettings(promptItem));

            var sb = new StringBuilder();
            foreach (var item in result)
            {
                sb.Append(item);
            }
            return sb.ToString();
        }

        public static PromptExecutionSettings BuildAIRequestSettings(PromptItem promptItem)
        {
            var aiSettings = new OpenAIPromptExecutionSettings
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
    }
}