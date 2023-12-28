using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Kernel.Helpers;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models;

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
                Constants.NeuCharOpenAI => await SkChatCompletionHelperService.WithAzureOpenAIChatCompletionService(promptItem, model),
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
                MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 200,
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
                    .CreateFunctionFromPrompt("TestPrompt", "PromptRange", promptParameter, completionPrompt)
                    .iWantToRun;
            var aiArguments = iWantToRun.CreateNewArguments().arguments;
            aiArguments["input"] = promptItem.Content;

            #region 用户自定义参数

            if (!string.IsNullOrWhiteSpace(promptItem.VariableDictJson))
            {
                // 如果有参数，前后缀不能为空
                if (string.IsNullOrWhiteSpace(promptItem.Prefix) || string.IsNullOrWhiteSpace(promptItem.Suffix))
                {
                    throw new NcfExceptionBase("前后缀不能为空");
                }

                // 读取参数并填充
                foreach (var (k, v) in (promptItem.VariableDictJson ?? "{}").GetObject<Dictionary<string, string>>())
                {
                    // aiArguments[$"{promptItem.Prefix}{k}{promptItem.Suffix}"] = v;
                    aiArguments[k] = v;
                }
            }

            #endregion

            var aiRequest = iWantToRun.CreateRequest(aiArguments, true);
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

            // 有期望结果， 进行自动打分
            if (!string.IsNullOrWhiteSpace(promptItem.ExpectedResultsJson))
            {
                this.RobotScoringAsync(promptResult.Id, promptItem.ExpectedResultsJson, false);
            }

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
                case AiPlatform.NeuCharOpenAI:
                    aiSettings.NeuCharOpenAIKeys = new NeuCharOpenAIKeys()
                    {
                        ApiKey = llmModel.ApiKey,
                        NeuCharOpenAIApiVersion = llmModel.ApiVersion, // SK中实际上没有用ApiVersion
                        NeuCharEndpoint = llmModel.Endpoint
                    };
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llmModel.ApiKey,
                        AzureOpenAIApiVersion = llmModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = llmModel.Endpoint
                    };
                    break;
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


        public async Task<PromptResult> RobotScoringAsync(int promptResultId, string expectedResultsJson, bool isRefresh)
        {
            List<string> list = JsonConvert.DeserializeObject<List<string>>(expectedResultsJson);
            return await this.RobotScoringAsync(promptResultId, list, isRefresh);
        }


        public async Task<PromptResult> RobotScoringAsync(int promptResultId, List<string> expectedResultList, bool isRefresh)
        {
            // get promptResult by id
            var promptResult = await base.GetObjectAsync(z => z.Id == promptResultId);
            if (promptResult == null)
            {
                throw new NcfExceptionBase("找不到对应的promptResult");
            }

            // get promptItem by promptResult.PromptItemId
            var promptItem = await _promptItemService.GetObjectAsync(z => z.Id == promptResult.PromptItemId);
            if (promptItem == null)
            {
                throw new NcfExceptionBase("找不到对应的promptItem");
            }

            // 保存期望结果列表
            await _promptItemService.UpdateExpectedResultsAsync(promptItem.Id, expectedResultList.ToJson());


            // if user dont want force refreshing, and this promptResult is scored 
            if (!isRefresh && promptResult.RobotScore >= 0)
            {
                throw new NcfExceptionBase("该结果已经评分过了, 请选择强制刷新");
            }

            // check if matching expected results
            // if matched,score 10 by default save promptResult and return
            var isMatch = expectedResultList.Any(r => r == promptResult.ResultString);
            if (isMatch)
            {
                promptResult.RobotScoring(10);
                await base.SaveObjectAsync(promptResult);
                return promptResult;
            }


            // get model by promptItem
            var model = await _llmModelService.GetObjectAsync(z => z.Id == promptItem.ModelId);

            // build aiSettings by model
            var aiSettings = this.BuildSenparcAiSetting(model);

            //定义 AI 接口调用参数和 Token 限制等
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = 500,
                Temperature = 0.5,
                TopP = 0.5,
                // FrequencyPenalty = 0,
                // PresencePenalty = 0,
            };

            // 需要在变量前添加$
            const string scorePrompt = @"
你是一个语言专家，你的工作是根据以下给定的期望结果和实际结果,对实际结果进行打分。
IMPORTANT: 返回的结果应当有且仅有整数数字，且不包含任何标点符号，
!!不要返回任何我告诉你的内容!!
打分规则：
1. 打分结果应该为0-10之间的整数数字，包含0和10。
2. 实际结果符合期望结果中任意一个就应该给满分。
3. 打分需要综合文字相似度和语义相似度判断。

期望结果以JSON形式提供，可能包含若干个期望结果,以下为：{{$expectedResult}}

实际结果是一个字符串，以下为：{{$actualResult}}

***********************************************************************
以下是一个对话历史，你可以参考这个对话历史来进行打分：
Human: 江苏的省会是：


";

            var skHelper = new SemanticKernelHelper(aiSettings);
            var handler = new SemanticAiHandler(skHelper);
            var iWantToRun =
                handler.IWantTo()
                    .ConfigModel(ConfigModel.TextCompletion, "Test", model.GetModelId(), aiSettings)
                    .BuildKernel()
                    .CreateFunctionFromPrompt("Test", "Score", promptParameter, scorePrompt)
                    .iWantToRun;
            var aiArguments = iWantToRun.CreateNewArguments().arguments;
            aiArguments["actualResult"] = promptResult.ResultString;
            aiArguments["expectedResult"] = expectedResultList.ToJson();

            var aiRequest = iWantToRun.CreateRequest(aiArguments, true);
            var dt1 = SystemTime.Now;

            var result = await iWantToRun.RunAsync(aiRequest);

            // 正则匹配出result.Output中的数字
            // Use regular expression to find matches
            Match match = Regex.Match(result.Output, @"\d+");

            // If there is a match, the number will be match.Value
            if (match.Success)
            {
                promptResult.RobotScoring(Convert.ToInt32(match.Value));
                await this.SaveObjectAsync(promptResult);

                return promptResult;
            }

            // SenparcTrace.SendCustomLog("自动打分结果匹配失败", $"原文为{result.Output}，分数匹配失败");

            throw new NcfExceptionBase($"自动打分结果匹配失败, 原文为{result.Output}，分数匹配失败，花费时间{SystemTime.DiffTotalMS(dt1)}ms");
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

        /// <summary>
        /// 更新promptItem的平均分和最高分
        /// </summary>
        /// <param name="promptItemId"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<Boolean> UpdateEvalScoreAsync(int promptItemId)
        {
            var promptItem = await _promptItemService.GetObjectAsync(p => p.Id == promptItemId);
            if (promptItem == null)
            {
                throw new NcfExceptionBase("找不到对应的promptItem");
            }

            List<PromptResult> promptResults = await this.GetFullListAsync(
                p => p.PromptItemId == promptItemId　&& (p.HumanScore >= 0 || p.RobotScore >= 0),
                p => p.Id, OrderingType.Ascending);

            double avg = promptResults.Average(r => r.HumanScore < 0 ? (r.RobotScore < 0 ? 0 : r.RobotScore) : r.HumanScore);
            promptItem.UpdateEvalAvgScore((int)avg);

            int max = promptResults.Max(r => r.HumanScore < 0 ? (r.RobotScore < 0 ? 0 : r.RobotScore) : r.HumanScore);

            promptItem.UpdateEvalMaxScore(max);

            await _promptItemService.SaveObjectAsync(promptItem);

            return true;

            // int sum = 0;
            // int cnt = 0;
            // foreach (var promptResult in promptResults)
            // {
            //     sum += promptResult.HumanScore < 0 ? (promptResult.RobotScore < 0 ? 0 : promptResult.RobotScore) : promptResult.HumanScore;
            //     cnt++;
            // }
            // if (cnt != 0)
            // {
            //     promptItem.UpdateEvalAvgScore((int)sum / cnt);
            //
            //     await _promptItemService.SaveObjectAsync(promptItem);
            // }
        }
    }
}