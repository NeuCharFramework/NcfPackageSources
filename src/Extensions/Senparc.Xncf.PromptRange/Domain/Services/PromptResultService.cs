using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Kernel.Helpers;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptResultService : ServiceBase<PromptResult>
    {
        public PromptResultService(
            IRepositoryBase<PromptResult> repo,
            IServiceProvider serviceProvider,
            LlmModelService llmModelService,
            PromptItemService promptItemService) : base(repo,
            serviceProvider)
        {
            _llmModelService = llmModelService;
            _promptItemService = promptItemService;
        }

        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
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
        /// 采用了SemanticKernel来实现
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
//
//       var userId = "Test";
//
//       var aiSettings = this.BuildSenparcAiSetting(model);
//       // //创建 AI Handler 处理器（也可以通过工厂依赖注入）
//       // var handler = new SemanticAiHandler(new SemanticKernelHelper(aiSettings));
//       //
//       // //定义 AI 接口调用参数和 Token 限制等
//       // var promptParameter = new PromptConfigParameter()
//       // {
//       //     MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 2000,
//       //     Temperature = promptItem.Temperature,
//       //     TopP = promptItem.TopP,
//       //     FrequencyPenalty = promptItem.FrequencyPenalty,
//       //     PresencePenalty = promptItem.PresencePenalty
//       // };
//
//       // 需要在变量前添加$
//       const string functionPrompt = @"请根据提示输出对应内容：
// {{$input}}";

            var dt1 = SystemTime.Now;
            var resp = model.ModelType switch
            {
                Constants.OpenAI => await SkChatCompletionHelperService.WithOpenAIChatCompletionService(promptItem, model),
                Constants.AzureOpenAI => await SkChatCompletionHelperService.WithAzureOpenAIChatCompletionService(promptItem, model),
                Constants.HuggingFace => await SkChatCompletionHelperService.WithHuggingFaceCompletionService(promptItem, model),
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
                promptItem.FullVersion, promptItem.Id);

            await base.SaveObjectAsync(promptResult);

            return promptResult;
        }


        public async Task<PromptResult> SenparcGenerateResultAsync(PromptItem promptItem)
        {
            // 从数据库中获取模型信息
            var model = await _llmModelService.GetObjectAsync(z => z.Id == promptItem.ModelId);
            if (model == null)
            {
                throw new NcfExceptionBase($"未找到模型{promptItem.ModelId}");
            }

            SenparcAiSetting aiSettings = this.BuildSenparcAiSetting(model);
            // //创建 AI Handler 处理器（也可以通过工厂依赖注入）
            // var handler = new SemanticAiHandler(new SemanticKernelHelper(aiSettings));
            //
            //定义 AI 接口调用参数和 Token 限制等
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 2000,
                Temperature = promptItem.Temperature,
                TopP = promptItem.TopP,
                FrequencyPenalty = promptItem.FrequencyPenalty,
                PresencePenalty = promptItem.PresencePenalty,
                StopSequences = (promptItem.StopSequences ?? "[]").GetObject<List<string>>(),
            };

            // 需要在变量前添加$
            const string completionPrompt = @"请根据提示输出对应内容：
{{$input}}";

            var skHelper = new SemanticKernelHelper(aiSettings);
            var handler = new SemanticAiHandler(skHelper);
            var iWantToRun =
                handler.IWantTo()
                    .ConfigModel(ConfigModel.TextCompletion, "Test", model.GetModelId(), aiSettings)
                    .BuildKernel()
                    .RegisterSemanticFunction("TestPrompt", "PromptRange", promptParameter, completionPrompt)
                    .iWantToRun;
            var skContext = iWantToRun.CreateNewContext().context;
            skContext.Variables["input"] = promptItem.Content;

            var aiRequest = iWantToRun.CreateRequest(skContext.Variables, true);
            var dt1 = SystemTime.Now;

            var result = await iWantToRun.RunAsync(aiRequest);

            // todo 计算token消耗
            // 简单计算
            // num_prompt_tokens = len(encoding.encode(string))
            // gap_between_send_receive = 15 * len(kwargs["messages"])
            // num_prompt_tokens += gap_between_send_receive
            var promptCostToken = 0;
            var resultCostToken = 0;

            var promptResult = new PromptResult(
                promptItem.ModelId, result.Output, SystemTime.DiffTotalMS(dt1),
                -1, -1, null, false, TestType.Text,
                promptCostToken, resultCostToken, promptCostToken + resultCostToken,
                promptItem.FullVersion, promptItem.Id);

            await base.SaveObjectAsync(promptResult);

            return promptResult;
        }


        public async Task<PromptResult> ManualScoreAsync(int id, int score)
        {
            #region validate

            //验证 score >= 0
            if (score < 0)
            {
                throw new NcfExceptionBase("分数不能小于0");
            }

            #endregion


            // 根据id搜索数据库
            var promptResult = await base.GetObjectAsync(result => result.Id == id);
            if (promptResult == null)
            {
                throw new NcfExceptionBase($"未找到{id}对应的结果");
            }

            promptResult.ManualScoring(score);
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
            }


            return aiSettings;
        }

        public async Task<string> RobotScore(int promptResultId, List<string> expectedResultList, bool isRefresh)
        {
            // get promptResult by id
            var promptResult = await base.GetObjectAsync(z => z.Id == promptResultId);

            // if user dont want force refreshing, and this promptResult is scored 
            if (!isRefresh && promptResult.RobotScore < 0)
            {
                throw new NcfExceptionBase("该结果已经评分过了, 请选择强制刷新");
            }

            // check if matching expected results
            var isMatch = expectedResultList.Any(r => r == promptResult.ResultString);
            if (isMatch)
            {
                promptResult.RobotScoring(10);
                await base.SaveObjectAsync(promptResult);
                return "10";
            }

            // get promptItem by promptResult
            var promptItem = await _promptItemService.GetObjectAsync(z => z.Id == promptResult.PromptItemId);

            // get model by promptItem
            var model = await _llmModelService.GetObjectAsync(z => z.Id == promptItem.ModelId);

            // build aiSettings by model
            var aiSettings = this.BuildSenparcAiSetting(model);

            //定义 AI 接口调用参数和 Token 限制等
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = 200,
                Temperature = 0.5,
                TopP = 0.5,
                // FrequencyPenalty = 0,
                // PresencePenalty = 0,
            };

            // 需要在变量前添加$
            const string scorePrompt = @"
你是一个语言专家，你的工作是根据以下给定的期望结果和实际结果,对实际结果进行打分。
IMPORTANT: 返回的结果应当有且仅有整数数字，且不包含任何标点符号
打分规则：
1. 打分结果应该为0-10之间的整数数字，包含0和10。
2. 实际结果符合期望结果中任意一个就应该给满分。
3. 打分需要综合文字相似度和语义相似度判断。

期望结果以JSON形式提供，可能包含若干个期望结果,以下为：{{$expectedResult}}

实际结果是一个字符串，以下为：{{$actualResult}}
";

            var skHelper = new SemanticKernelHelper(aiSettings);
            var handler = new SemanticAiHandler(skHelper);
            var iWantToRun =
                handler.IWantTo()
                    .ConfigModel(ConfigModel.TextCompletion, "Test", model.GetModelId(), aiSettings)
                    .BuildKernel()
                    .RegisterSemanticFunction("Test", "Score", promptParameter, scorePrompt)
                    .iWantToRun;
            var skContext = iWantToRun.CreateNewContext().context;
            skContext.Variables["actualResult"] = promptResult.ResultString;
            skContext.Variables["expectedResult"] = expectedResultList.ToJson();

            var aiRequest = iWantToRun.CreateRequest(skContext.Variables, true);
            var dt1 = SystemTime.Now;

            var result = await iWantToRun.RunAsync(aiRequest);

            // 正则匹配出result.Output中的数字
            // Use regular expression to find matches
            Match match = Regex.Match(result.Output, @"\d+");

            // If there is a match, the number will be match.Value
            if (match.Success)
            {
                await this.SaveObjectAsync(promptResult.RobotScoring(Convert.ToInt32(match.Value)));
                return match.Value;
            }

            SenparcTrace.SendCustomLog("自动打分结果匹配失败", $"原文为{result.Output}，分数匹配失败");
            return "0";
        }

        public async Task<Boolean> BatchDeleteWithItemId(int promptItemId)
        {
            try
            {
                await this.DeleteAllAsync(res => res.PromptItemId == promptItemId);
            }
            catch (Exception e)
            {
                throw new NcfExceptionBase($"删除{promptItemId}对应的结果失败, msg: {e.Message}");
            }

            return true;
        }
    }
}