using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using log4net.Util;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
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
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptResultService : ServiceBase<PromptResult>
    {
        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly LlModelService _llModelService;

        public PromptResultService(
            IRepositoryBase<PromptResult> repo,
            IServiceProvider serviceProvider,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            LlModelService llModelService
            ) : base(repo,
            serviceProvider)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _llModelService = llModelService;
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
            //string completionPrompt = $@"请根据提示输出对应内容:
            //{promptItem.Content}";
            string completionPrompt = $@"{promptItem.Content}";

            // 从数据库中获取模型信息
            var model = promptItem.AIModelDto;
            // 构建生成AI设置
            SenparcAiSetting aiSettings = this.BuildSenparcAiSetting(model);

            //TODO: model 加上模型的类型：Chat/TextCompletion/TextToImage 等
            ConfigModel configModel = _llModelService.ConvertToConfigModel(model.ConfigModelType);

            // 创建 AI Handler 处理器（也可以通过工厂依赖注入）
            var handler = new SemanticAiHandler(aiSettings);
            var iWantToRun =
                handler.IWantTo(aiSettings)
                    // todo 替换为真实用户名，可能需要从NeuChar获取？
                    .ConfigModel(configModel, "SenparcGenerateResult")
                    .BuildKernel()
                    .CreateFunctionFromPrompt(completionPrompt, promptParameter)
                    .iWantToRun;
            var aiArguments = iWantToRun.CreateNewArguments().arguments;
            //aiArguments["input"] = promptItem.Content;

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


        public async Task<PromptResult> ManualScoreAsync(int id, decimal score)
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
                        NeuCharEndpoint = llModel.Endpoint,
                        ModelName = new AI.Entities.Keys.ModelName()
                        {
                            Chat = llModel.ModelId,
                            TextCompletion = llModel.ModelId,
                            Embedding = llModel.ModelId,
                            ImageToText = llModel.ModelId,
                            TextToImage = llModel.ModelId
                        }
                    };
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        AzureOpenAIApiVersion = llModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = llModel.Endpoint,
                        DeploymentName = llModel.DeploymentName,
                        ModelName = new AI.Entities.Keys.ModelName()
                        {
                            Chat = llModel.ModelId,
                            TextCompletion = llModel.ModelId,
                            Embedding = llModel.ModelId,
                            ImageToText = llModel.ModelId,
                            TextToImage = llModel.ModelId
                        }
                    };
                    break;
                case AiPlatform.AzureOpenAI:
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        AzureOpenAIApiVersion = llModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = llModel.Endpoint,
                        DeploymentName = llModel.DeploymentName,
                        ModelName = new AI.Entities.Keys.ModelName()
                        {
                            Chat = llModel.ModelId,
                            TextCompletion = llModel.ModelId,
                            Embedding = llModel.ModelId,
                            ImageToText = llModel.ModelId,
                            TextToImage = llModel.ModelId
                        }
                    };
                    break;
                case AiPlatform.HuggingFace:
                    aiSettings.HuggingFaceKeys = new HuggingFaceKeys()
                    {
                        Endpoint = llModel.Endpoint,
                        ModelName = new AI.Entities.Keys.ModelName()
                        {
                            Chat = llModel.ModelId,
                            TextCompletion = llModel.ModelId,
                            Embedding = llModel.ModelId,
                            ImageToText = llModel.ModelId,
                            TextToImage = llModel.ModelId
                        }
                    };
                    break;
                case AiPlatform.OpenAI:
                    aiSettings.OpenAIKeys = new OpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        OrganizationId = llModel.OrganizationId,
                        ModelName = new AI.Entities.Keys.ModelName()
                        {
                            Chat = llModel.ModelId,
                            TextCompletion = llModel.ModelId,
                            Embedding = llModel.ModelId,
                            ImageToText = llModel.ModelId,
                            TextToImage = llModel.ModelId
                        }
                    };
                    break;
                case AiPlatform.FastAPI:
                    aiSettings.FastAPIKeys = new FastAPIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        Endpoint = llModel.Endpoint,
                        //OrganizationId = aiModel.OrganizationId
                        ModelName = new AI.Entities.Keys.ModelName()
                        {
                            Chat = llModel.ModelId,
                            TextCompletion = llModel.ModelId,
                            Embedding = llModel.ModelId,
                            ImageToText = llModel.ModelId,
                            TextToImage = llModel.ModelId
                        }
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

        /// <summary>
        /// AI 打分
        /// </summary>
        /// <param name="promptResultId"></param>
        /// <param name="isRefresh"></param>
        /// <param name="expectedResultList"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<PromptResult> RobotScoringAsync(int promptResultId, bool isRefresh, List<string> expectedResultList)
        {
            // 需要在变量前添加$
            const int MAX_SCORE = 10;
            const string MAX_SOCRE_STR = "10";


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
            var isMatch = expectedResultList.Any(r => r == promptResult.ResultString || (r.StartsWith("=") && r.Substring(1, r.Length - 1) == promptResult.ResultString));
            if (isMatch)
            {
                promptResult.RobotScoring(MAX_SCORE);
                promptResult.FinalScoring(promptResult.RobotScore);
                await base.SaveObjectAsync(promptResult);
                return promptResult;
            }

            //TODO：添加不等号规则



            const string scorePrompt = $@"[背景]
你是一个语言专家和 AI 生成结果的评分专家，你的工作是根据以下给定的[期望结果],对[实际结果]进行打分。
[期望结果]将以 JSON 的数组形式提供，数组中包含了若干个期望结果的描述。

[打分规则]
1. 将[实际结果]与[期望结果]中的每一项进行比较，并返回最高分。
2. 当某一项获得最高分（{MAX_SOCRE_STR}）的时候，停止对剩余规则的判断，直接返回 {MAX_SOCRE_STR} 分。
3. 当[实际结果]完全等于[期望结果]数组的中任意一项时，就给满分（{MAX_SOCRE_STR}）。
4. 根据[期望结果]中每一项的描述内容来判定[实际结果]的准确性，判断依据为：
  2.1. 综合文字相似度、语义相似度和所描述的匹配度判断
  2.2. 打分结果应该为0-10之间的数字，包含 0 和 {MAX_SOCRE_STR}，至多为 1 位小数。
5. 当[期望结果]中的字符串以""=""符号开头时，[实际结果]必须与之完全相等才能得满分（{MAX_SOCRE_STR} 分），否则最多得 5 分。

IMPORTANT: 返回的结果必须为0-10的数字，且不包含任何标点符号。
!!不要返回任何我告诉你的内容!!

[期望结果]
{{{{$expectedResult}}}}

[实际结果]
{{{{$actualResult}}}}

打分结果：";

            // 获取模型
            var model = promptItem.AIModelDto;

            // build aiSettings by model
            var aiSettings = this.BuildSenparcAiSetting(model);
            //TODO:可以设置一个默认 PromptRange 的值，或者使用配置来指定打分的 AI，而不是使用同一个模型。

            var expectedResult = expectedResultList.ToJson();

            //定义 AI 接口调用参数和 Token 限制等
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = promptItem.AIModelDto.MaxToken + expectedResult.Length + scorePrompt.Length + 500,
                Temperature = 0.2,
                TopP = 0.2,
                // FrequencyPenalty = 0,
                // PresencePenalty = 0,
            };

            ConfigModel configModel = _llModelService.ConvertToConfigModel(model.ConfigModelType);

            var handler = new SemanticAiHandler(aiSettings);
            var iWantToRun =
                handler.IWantTo(aiSettings)
                    .ConfigModel(configModel, "AIScoring")
                    .BuildKernel()
                    .CreateFunctionFromPrompt(scorePrompt, promptParameter)
                    .iWantToRun;
            var aiArguments = iWantToRun.CreateNewArguments().arguments;
            aiArguments["actualResult"] = promptResult.ResultString;
            aiArguments["expectedResult"] = expectedResult;

            var aiRequest = iWantToRun.CreateRequest(aiArguments, true);
            var dt1 = SystemTime.Now;

            var result = await iWantToRun.RunAsync(aiRequest);
            SenparcTrace.SendCustomLog("自动打分结束", $"模型返回为{result.Output}，花费时间{SystemTime.DiffTotalMS(dt1)}ms");

            // 正则匹配出result.Output中的数字
            // Use regular expression to find matches

            // 匹配 MAX_SCORE，后面可以跟 0-2 位的小数
            string pattern = "^(10(\\.0{1,2})?|[1-9](\\.\\d{1,2})?|0(\\.\\d{1,2})?)$";
            //@"^100(\.[0-9]{1,2})?|([0-9]{1,2})(\.[0-9]{1,2})?$";
            //"^(100(\.0{1,2})?|[1-9]?\d(\.\d{1,2})?|0(\.\d{1,2})?)$"
            //^(10(\.0{1,2})?|[1-9](\.\d{1,2})?|0(\.\d{1,2})?)$
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

            #region error 打分结果不在 0-MAX_SCORE 之间

            score = score > MAX_SCORE ? MAX_SCORE : (score < 0 ? 0 : score);

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