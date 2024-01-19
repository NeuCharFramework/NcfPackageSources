using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptResultService : ServiceBase<PromptResult>
    {
        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;

        public PromptResultService(
            IRepositoryBase<PromptResult> repo,
            IServiceProvider serviceProvider,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService) : base(repo,
            serviceProvider)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
        }


        public async Task<List<PromptResultDto>> GetByItemId(int promptItemId)
        {
            // var promptItem = await _promptItemService.GetObjectAsync(p => p.Id == promptItemId)
            //     ?? throw new NcfExceptionBase($"未找到{promptItemId}对应的提示词");

            var resultList = (await this.GetFullListAsync(
                p => p.PromptItemId == promptItemId,
                p => p.Id,
                OrderingType.Ascending));

            var dtoList = resultList
                .Select(p => this.Mapper.Map<PromptResultDto>(p))
                .ToList();

            return dtoList;
        }


        public async Task<PromptResultDto> SenparcGenerateResultAsync(PromptItemDto promptItem)
        {
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

            // 从数据库中获取模型信息
            var model = promptItem.AIModelDto;
            // 构建生成AI设置
            SenparcAiSetting aiSettings = this.BuildSenparcAiSetting(model);

            // 创建 AI Handler 处理器（也可以通过工厂依赖注入）
            var handler = new SemanticAiHandler(aiSettings);
            var iWantToRun =
                handler.IWantTo(aiSettings)
                    // todo 替换为真实用户名，可能需要从NeuChar获取？
                    .ConfigModel(ConfigModel.TextCompletion, "Test", model.DeploymentName, aiSettings)
                    .BuildKernel()
                    .CreateFunctionFromPrompt(completionPrompt, promptParameter)
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
                -1, -1, -1, TestType.Text,
                promptCostToken, resultCostToken, promptCostToken + resultCostToken,
                promptItem.FullVersion, promptItem.Id);

            await base.SaveObjectAsync(promptResult);

            // 有期望结果， 进行自动打分
            if (!string.IsNullOrWhiteSpace(promptItem.ExpectedResultsJson))
            {
                await this.RobotScoringAsync(promptResult.Id, false, promptItem.ExpectedResultsJson);
            }

            return this.Mapper.Map<PromptResultDto>(promptResult);
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
            var promptResult = await base.GetObjectAsync(result => result.Id == id) ??
                               throw new NcfExceptionBase($"未找到{id}对应的结果");

            promptResult.ManualScoring(score);
            promptResult.FinalScoring(score);

            await base.SaveObjectAsync(promptResult);

            return promptResult;
        }


        /// <summary>
        /// 构造 SenparcAiSetting, 在两个地方使用
        /// </summary>
        /// <param name="llModel"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public SenparcAiSetting BuildSenparcAiSetting(AIModelDto llModel)
        {
            var aiSettings = new SenparcAiSetting
            {
                AiPlatform = llModel.AiPlatform
            };

            switch (aiSettings.AiPlatform)
            {
                case AiPlatform.NeuCharAI:
                    aiSettings.NeuCharAIKeys = new NeuCharAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        NeuCharAIApiVersion = llModel.ApiVersion, // SK中实际上没有用ApiVersion
                        NeuCharEndpoint = llModel.Endpoint
                    };
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        AzureOpenAIApiVersion = llModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = llModel.Endpoint
                    };
                    break;
                case AiPlatform.AzureOpenAI:
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        AzureOpenAIApiVersion = llModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = llModel.Endpoint
                    };
                    break;
                case AiPlatform.HuggingFace:
                    aiSettings.HuggingFaceKeys = new HuggingFaceKeys()
                    {
                        Endpoint = llModel.Endpoint
                    };
                    break;
                case AiPlatform.OpenAI:
                    aiSettings.OpenAIKeys = new OpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        OrganizationId = llModel.OrganizationId
                    };
                    break;
                default:
                    throw new NcfExceptionBase($"暂时不支持{aiSettings.AiPlatform}类型");
            }


            return aiSettings;
        }


        public async Task<PromptResult> RobotScoringAsync(int promptResultId, bool isRefresh, string expectedResultsJson)
        {
            List<string> list = //JsonConvert.DeserializeObject<>(expectedResultsJson);
                expectedResultsJson.GetObject<List<string>>();
            return await this.RobotScoringAsync(promptResultId, isRefresh, list);
        }


        public async Task<PromptResult> RobotScoringAsync(int promptResultId, bool isRefresh, List<string> expectedResultList)
        {
            // get promptResult by id
            var promptResult = await this.GetObjectAsync(z => z.Id == promptResultId)
                               ?? throw new NcfExceptionBase("找不到对应的promptResult");


            // get promptItem by promptResult.PromptItemId
            var promptItem = await _promptItemService.GetAsync(promptResult.PromptItemId)
                             ?? throw new NcfExceptionBase("找不到对应的promptItem");

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
                promptResult.RobotScoring(100);
                promptResult.FinalScoring(promptResult.RobotScore);
                await base.SaveObjectAsync(promptResult);
                return promptResult;
            }


            // 获取模型
            var model = promptItem.AIModelDto;

            // build aiSettings by model
            var aiSettings = this.BuildSenparcAiSetting(model);

            //定义 AI 接口调用参数和 Token 限制等
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = /*model.MaxToken > 0 ? model.MaxToken :*/ 2000,
                Temperature = 0.2,
                TopP = 0.2,
                // FrequencyPenalty = 0,
                // PresencePenalty = 0,
            };

            // 需要在变量前添加$
            const string scorePrompt = @"
你是一个语言专家，你的工作是根据以下给定的期望结果和实际结果,对实际结果进行打分。
IMPORTANT: 返回的结果必须为0-10的整数数字，且不包含任何标点符号，
!!不要返回任何我告诉你的内容!!
打分规则：
1. 打分结果应该为0-100之间的数字，包含0和100，至多为2位小数。
2. 实际结果符合期望结果中任意一个就应该给满分。
3. 打分需要综合文字相似度和语义相似度判断。

期望结果以JSON形式提供，可能包含若干个期望结果,以下为：{{$expectedResult}}

实际结果是一个字符串，以下为：{{$actualResult}}

********************************************************************************
";

            var handler = new SemanticAiHandler(aiSettings);
            var iWantToRun =
                handler.IWantTo(aiSettings)
                    .ConfigModel(ConfigModel.TextCompletion, "Test", model.DeploymentName, aiSettings)
                    .BuildKernel()
                    .CreateFunctionFromPrompt(scorePrompt, promptParameter)
                    .iWantToRun;
            var aiArguments = iWantToRun.CreateNewArguments().arguments;
            aiArguments["actualResult"] = promptResult.ResultString;
            aiArguments["expectedResult"] = expectedResultList.ToJson();

            var aiRequest = iWantToRun.CreateRequest(aiArguments, true);
            var dt1 = SystemTime.Now;

            var result = await iWantToRun.RunAsync(aiRequest);
            SenparcTrace.SendCustomLog("自动打分结束", $"模型返回为{result.Output}，花费时间{SystemTime.DiffTotalMS(dt1)}ms");

            // 正则匹配出result.Output中的数字
            // Use regular expression to find matches

            // 匹配100，后面可以跟 0-2 位的小数
            string pattern = @"^100(\.[0-9]{1,2})?|([0-9]{1,2})(\.[0-9]{1,2})?$";
            Match match = Regex.Match(result.Output, pattern);

            // If there is a match, the number will be match.Value
            if (!match.Success)
            {
                SenparcTrace.SendCustomLog("自动打分结束", $"原文为{result.Output}，分数匹配失败");

                throw new NcfExceptionBase($"自动打分结果匹配失败, 被打分的结果字符串为{promptResult.ResultString}, 模型返回为{result.Output}，");
            }

            bool success = Decimal.TryParse(match.Value, out var score);
            if (!success)
            {
                throw new NcfExceptionBase($"自动打分结果匹配失败, 被打分的结果字符串为{promptResult.ResultString}, 模型返回为{result.Output}，");
            }

            #region error 打分结果不在 0-100 之间

            score = score > 100 ? 100 : score < 0 ? 0 : score;

            #endregion

            promptResult.RobotScoring(score);
            promptResult.FinalScoring(promptResult.RobotScore);

            await this.SaveObjectAsync(promptResult);

            return promptResult;
        }

        public async Task<Boolean> BatchDeleteWithItemId(List<int> ids)
        {
            foreach (var id in ids)
            {
                try
                {
                    await this.DeleteAllAsync(res => res.PromptItemId == id);
                }
                catch (Exception e)
                {
                    throw new NcfExceptionBase($"删除{id}对应的结果失败, msg: {e.Message}");
                }
            }

            return true;
        }

        /// <summary>
        /// 更新promptItem的平均分和最高分
        /// </summary>
        /// <param name="promptItemId"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<bool> UpdateEvalScoreAsync(int promptItemId)
        {
            var promptItem = await _promptItemService.GetObjectAsync(p => p.Id == promptItemId) ??
                             throw new NcfExceptionBase("找不到对应的promptItem");

            List<PromptResult> promptResults = await this.GetFullListAsync(
                p => p.PromptItemId == promptItemId && p.FinalScore >= 0
            );

            if (promptResults.Count == 0)
            {
                // 没有结果
                return false;
            }

            decimal avg = promptResults.Average(r => r.FinalScore);
            promptItem.UpdateEvalAvgScore(avg);

            decimal max = promptResults.Max(r => r.FinalScore);
            promptItem.UpdateEvalMaxScore(max);

            await _promptItemService.SaveObjectAsync(promptItem);

            return true;
        }
    }
}