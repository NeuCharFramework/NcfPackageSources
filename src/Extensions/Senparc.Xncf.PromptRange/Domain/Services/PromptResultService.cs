using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.HuggingFace.TextCompletion;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.Exceptions;
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
            var llmModel = await _llmModelService.GetObjectAsync(z => z.Id == promptItem.ModelId);
            if (llmModel == null)
            {
                throw new NcfExceptionBase($"未找到模型{promptItem.ModelId}");
            }

            var userId = "Test";
            var modelName = LlmModelHelper.GetAzureModelName(llmModel.Name);
            var aiSettings = this.BuildSenparcAiSetting(llmModel);
            //创建 AI Handler 处理器（也可以通过工厂依赖注入）
            var handler = new SemanticAiHandler(new AI.Kernel.Helpers.SemanticKernelHelper(aiSettings));

            //定义 AI 接口调用参数和 Token 限制等
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 2000,
                Temperature = promptItem.Temperature,
                TopP = promptItem.TopP,
                FrequencyPenalty = promptItem.FrequencyPenalty,
                PresencePenalty = promptItem.PresencePenalty
            };

            // 需要在变量前添加$
            var functionPrompt = @"请根据提示输出对应内容：\n{{$input}}";

            // var kernelBuilder = helper.ConfigTextCompletion(userId, modelName, aiSettings);
            // kernelBuilder.WithAzureTextCompletionService(llmModel.Name,llmModel.Endpoint,llmModel.ApiKey);
            var iWantToRun = handler.IWantTo()
                .ConfigModel(ConfigModel.TextCompletion, userId, modelName, aiSettings)
                .BuildKernel()
                .RegisterSemanticFunction("TestPrompt", "PromptRange", promptParameter, functionPrompt)
                .iWantToRun;


            // //todo which function to use? completion or chat?
            // var func = "chat/completions";
            // // var func = "completions";
            //
            //
            // // construct the target url
            // // 枚举Constants.ModelTypeEnum, 生成对应的url
            // string aiUrl;
            // if (Enum.TryParse(llmModel.ModelType, out AiPlatform aiPlatform))
            // {
            //   switch (aiPlatform)
            //   {
            //     case AiPlatform.AzureOpenAI:
            //       //todo: validate the parameters
            //       aiUrl = $"{llmModel.Endpoint}/{modelName}/{func}?api-version={llmModel.ApiVersion}";
            //       break;
            //     case AiPlatform.HuggingFace:
            //       aiUrl = $"{llmModel.Endpoint}/{func}?api-version={llmModel.ApiVersion}";
            //       break;
            //     case AiPlatform.OpenAI:
            //       aiUrl = $"https://api.openai.com/v1/{func}";
            //       break;
            //     default: //暂时不支持其他的
            //       aiUrl = "";
            //       logger.Append("未知的模型类型");
            //       break;
            //   }
            // }
            // else
            // {
            //   aiUrl = "";
            //   logger.Append("未知的模型类型");
            // }

            // 试试先用sk的原生hf
            var conn = new HuggingFaceTextCompletion(llmModel.Name, endpoint:llmModel.Endpoint);
            var aiRequestSettings = new AIRequestSettings() {
                ExtensionData = new Dictionary<string, object>
                {
                    { "Temperature", 0.7 },
                    { "TopP", 0.5 },
                    { "MaxTokens", 3000 }
                }
            };
            var resp = await conn.CompleteAsync(promptItem.Content,aiRequestSettings);
            

            // var skContext = iWantToRun.CreateNewContext().context;
            // // var context = iWantToRun.CreateNewContext();
            // skContext.Variables["input"] = promptItem.Content;
            //
            // var aiRequest = iWantToRun.CreateRequest(skContext.Variables, true);
            // var result = await iWantToRun.RunAsync(aiRequest);

            var dt1 = SystemTime.Now;

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
    }
}