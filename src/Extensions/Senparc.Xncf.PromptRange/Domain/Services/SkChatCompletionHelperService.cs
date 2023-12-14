using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.HuggingFace.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Senparc.Xncf.PromptRange.Models;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public static class SkChatCompletionHelperService
    {
        
        public static async Task<string> WithOpenAIChatCompletionService(PromptItem promptItem, LlmModel model)
        {
            OpenAIChatCompletion chatGPT = new(
                modelId: model.GetModelId(),
                apiKey: model.ApiKey,
                organization: model.OrganizationId
            );
            // add system prompt
            ChatHistory chatHistory = chatGPT.CreateNewChat(promptItem.ChatSystemPrompt);
            // add prompt
            chatHistory.AddUserMessage(promptItem.Content);
            string reply = await chatGPT.GenerateMessageAsync(chatHistory, BuildAIRequestSettings(promptItem));
            // chatHistory.AddAssistantMessage(reply);
            // return chatHistory.Last().Content;
            return reply;
        }

        public static async Task<string> WithAzureOpenAIChatCompletionService(PromptItem promptItem, LlmModel model)
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
        public static async Task<string> WithHuggingFaceCompletionService(PromptItem promptItem, LlmModel model)
        {
            var conn = new HuggingFaceTextCompletion(model.GetModelId(), endpoint: model.Endpoint);
            // var aiRequestSettings = BuildAIRequestSettings(promptItem);
            return await conn.CompleteAsync(promptItem.Content, BuildAIRequestSettings(promptItem));
        }

        public static OpenAIRequestSettings BuildAIRequestSettings(PromptItem promptItem)
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
    }
}