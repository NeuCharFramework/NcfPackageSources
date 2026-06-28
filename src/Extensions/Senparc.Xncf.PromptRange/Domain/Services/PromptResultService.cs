using log4net.Util;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NetTopologySuite.IO;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptResultService : ServiceBase<PromptResult>
    {
        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly LlModelService _llModelService;
        private readonly PromptResultChatService _promptResultChatService;
        private readonly Lazy<AIModelService> _aiModelService;

        public PromptResultService(
            IRepositoryBase<PromptResult> repo,
            IServiceProvider serviceProvider,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            LlModelService llModelService,
            PromptResultChatService promptResultChatService,
            Lazy<AIModelService> aiModelService) : base(repo,
            serviceProvider)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _llModelService = llModelService;
            _promptResultChatService = promptResultChatService;
            _aiModelService = aiModelService;
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

        /// <summary>
        /// 获取 AI 生成结果
        /// </summary>
        /// <param name="promptItem"></param>
        /// <param name="userMessage"></param>
        /// <param name="chatHistory"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<PromptResultDto> SenparcGenerateResultAsync(
            PromptItemDto promptItem,
            string userMessage = null,
            List<ChatMessageDto> chatHistory = null,
            Action<PromptResultStreamEvent> onStreamEvent = null)
        {
            // 需要在变量前添加$
            //string completionPrompt = $@"请根据提示输出对应内容:
            //{promptItem.Content}";
            string completionPrompt = $@"{promptItem.Content}";

            //定义 AI 接口调用参数和 Token 限制等
            var promptParameter = new ChatOptions()
            {
                Instructions = completionPrompt,
                MaxOutputTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 2000,
                Temperature = promptItem.Temperature,
                TopP = promptItem.TopP,
                FrequencyPenalty = promptItem.FrequencyPenalty,
                PresencePenalty = promptItem.PresencePenalty,
                StopSequences = (promptItem.StopSequences ?? "[]").GetObject<List<string>>() ?? new List<string>(),
            };

            // 生成替换参数后的 SystemMessage（用于保存到数据库）
            // 如果 Prompt 内容包含参数占位符（如 {{$variableName}}），进行参数替换
            string systemMessage = completionPrompt;
            if (!string.IsNullOrWhiteSpace(promptItem.VariableDictJson) &&
                !string.IsNullOrWhiteSpace(promptItem.Prefix) &&
                !string.IsNullOrWhiteSpace(promptItem.Suffix))
            {
                // 读取参数并替换 Prompt 内容中的占位符
                var variableDict = (promptItem.VariableDictJson ?? "{}").GetObject<Dictionary<string, string>>()
                    ?? new Dictionary<string, string>();
                foreach (var (key, value) in variableDict)
                {
                    // 替换格式：{Prefix}{key}{Suffix} -> value
                    // 例如：{{$variableName}} -> actualValue
                    string placeholder = $"{promptItem.Prefix}{key}{promptItem.Suffix}";
                    systemMessage = systemMessage.Replace(placeholder, value ?? string.Empty);
                }
            }

            var hasUserMessage = !string.IsNullOrWhiteSpace(userMessage);
            var normalizedUserMessage = hasUserMessage ? userMessage : null;

            // Chat 模式：Instructions 为系统 Prompt；Single 模式：Prompt 作为用户消息发送
            promptParameter.Instructions = hasUserMessage ? systemMessage : null;

            // 从数据库中获取模型信息
            var model = promptItem.AIModelDto
                ?? throw new NcfExceptionBase($"找不到 PromptItem（{promptItem.FullVersion}）对应的 AI 模型配置");
            SenparcAiSetting aiSettings = _aiModelService.Value.BuildSenparcAiSetting(model);

            var agentOptions = BuildChatClientAgentOptions(promptParameter);
            var handler = new AgentAiHandler(aiSettings).IWantTo();
            var iWantToRun = await handler
                .ConfigChatModel($"PromptRange_{promptItem.Id}", agentOptions)
                .BuildKernelWithAgentSessionAsync();

            #region 用户自定义参数

            var agentSession = iWantToRun.Kernel.AgentSession
                ?? throw new NcfExceptionBase("AgentKernel 未创建 AgentSession，请检查 AI 模型配置是否为 Chat 类型");

            var history = BuildChatMessagesFromDto(chatHistory);
            agentSession.SetInMemoryChatHistory(history);

            if (!string.IsNullOrWhiteSpace(promptItem.VariableDictJson))
            {
                // 如果有参数，前后缀不能为空
                if (string.IsNullOrWhiteSpace(promptItem.Prefix) || string.IsNullOrWhiteSpace(promptItem.Suffix))
                {
                    throw new NcfExceptionBase("前后缀不能为空");
                }
            }

            #endregion
            SenparcAiResult aiResult = null;
            UsageDetails streamUsageDetails = null;
            var dt1 = SystemTime.Now;
            void HandleStreamUpdate(AgentResponseUpdate update)
            {
                if (update == null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(update.Text) && onStreamEvent != null)
                {
                    onStreamEvent(new PromptResultStreamEvent
                    {
                        EventType = "chunk",
                        Text = update.Text,
                        IsFinal = false
                    });
                }

                if (update.Contents?.FirstOrDefault(z => z is UsageContent) is UsageContent usageContent
                    && usageContent.Details != null)
                {
                    streamUsageDetails = usageContent.Details;
                }
            }

            // Chat 模式使用 userMessage，Single 模式直接执行解析后的 Prompt 内容
            var runPrompt = hasUserMessage ? normalizedUserMessage : systemMessage;
            if (string.IsNullOrWhiteSpace(runPrompt))
            {
                throw new NcfExceptionBase("Prompt 内容不能为空");
            }

            aiResult = onStreamEvent == null
                ? await iWantToRun.RunChatAsync(runPrompt, agentSession)
                : await iWantToRun.RunChatAsync(runPrompt, agentSession, HandleStreamUpdate);
            var aiOutput = aiResult.OutputString;
            var usageInfo = PromptUsageHelper.ResolveUsage(streamUsageDetails ?? TryGetUsageFromResult(aiResult));
            var promptCostToken = usageInfo.PromptTokens;
            var resultCostToken = usageInfo.CompletionTokens;

            // 判断是否为聊天模式
            var isChatMode = hasUserMessage;
            var resultMode = isChatMode ? ResultMode.Chat : ResultMode.Single;

            // 如果是聊天模式，保存 SystemMessage；否则为 null
            var promptResult = new PromptResult(
                promptItem.ModelId, aiOutput, SystemTime.DiffTotalMS(dt1),
                -1, -1, -1, TestType.Text,
                promptCostToken, resultCostToken, promptCostToken + resultCostToken,
                promptItem.FullVersion, promptItem.Id,
                resultMode,
                isChatMode ? systemMessage : null); // 只在聊天模式时保存 SystemMessage

            await base.SaveObjectAsync(promptResult);

            // 如果是聊天模式，保存对话记录
            if (isChatMode && !string.IsNullOrWhiteSpace(normalizedUserMessage) && !string.IsNullOrWhiteSpace(aiOutput))
            {
                var chatMessages = new List<ChatMessageDto>();

                // 如果有历史记录，先添加历史记录
                if (chatHistory != null && chatHistory.Count > 0)
                {
                    chatMessages.AddRange(chatHistory);
                }

                // 添加当前对话（用户消息和 AI 响应）
                chatMessages.Add(new ChatMessageDto { Role = "user", Content = normalizedUserMessage });
                chatMessages.Add(new ChatMessageDto { Role = "assistant", Content = aiOutput });

                await _promptResultChatService.AddChatMessagesAsync(promptResult.Id, chatMessages);
            }

            // 有期望结果， 进行自动打分
            if (promptItem.isAIGrade && !string.IsNullOrWhiteSpace(promptItem.ExpectedResultsJson))
            {
                await this.RobotScoringAsync(promptResult.Id, false, promptItem.ExpectedResultsJson);
            }

            if (onStreamEvent != null)
            {
                onStreamEvent(new PromptResultStreamEvent
                {
                    EventType = "final",
                    PromptResultId = promptResult.Id,
                    Text = aiOutput,
                    IsFinal = true,
                    PromptTokens = promptCostToken,
                    CompletionTokens = resultCostToken,
                    TotalTokens = usageInfo.TotalTokens,
                    ResponseMilliseconds = (int)Math.Round(promptResult.CostTime)
                });
            }

            return this.Mapper.Map<PromptResultDto>(promptResult);
        }

        /// <summary>
        /// 继续聊天：在现有 PromptResult 中追加对话记录，不创建新的 PromptResult
        /// </summary>
        /// <param name="promptResultId">现有的 PromptResult ID</param>
        /// <param name="userMessage">用户消息</param>
        /// <returns>返回新追加的对话记录（用户消息和 AI 回复）</returns>
        public async Task<List<PromptResultChatDto>> ContinueChatAsync(
            int promptResultId,
            string userMessage,
            Action<PromptResultStreamEvent> onStreamEvent = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                throw new NcfExceptionBase("用户消息不能为空");
            }

            // 获取现有的 PromptResult
            var promptResult = await this.GetObjectAsync(p => p.Id == promptResultId)
                ?? throw new NcfExceptionBase($"未找到 ID 为 {promptResultId} 的 PromptResult");

            // 验证是否为聊天模式
            if (promptResult.Mode != ResultMode.Chat)
            {
                throw new NcfExceptionBase("只有聊天模式的 PromptResult 才能继续聊天");
            }

            // 获取 PromptItem
            var promptItem = await _promptItemService.GetAsync(promptResult.PromptItemId);

            // 获取历史对话记录
            var chatHistory = await _promptResultChatService.GetByPromptResultIdAsync(promptResultId);
            // var chatHistoryForAI = chatHistory.Select(c => new ChatMessageDto
            // {
            //     Role = c.RoleType == ChatRoleType.User ? "user" : "assistant",
            //     Content = c.Content
            // }).ToList();

            //// 定义 AI 接口调用参数
            //var promptParameter = new PromptConfigParameter()
            //{
            //    MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 200,
            //    Temperature = promptItem.Temperature,
            //    TopP = promptItem.TopP,
            //    FrequencyPenalty = promptItem.FrequencyPenalty,
            //    PresencePenalty = promptItem.PresencePenalty,
            //    StopSequences = (promptItem.StopSequences ?? "[]").GetObject<List<string>>(),
            //};

            // 优先使用保存的 SystemMessage，如果没有则使用当前的 Prompt 内容
            // 这样可以确保即使 Prompt 内容或参数变化，继续对话时也使用最初保存的 SystemMessage
            string completionPrompt;
            if (!string.IsNullOrWhiteSpace(promptResult.SystemMessage))
            {
                // 使用保存的 SystemMessage（已完成参数替换）
                completionPrompt = promptResult.SystemMessage;
            }
            else
            {
                // 降级方案：如果没有保存的 SystemMessage，使用当前的 Prompt 内容
                // 这种情况可能发生在旧数据或 Single 模式的数据中
                completionPrompt = $@"{promptItem.Content}";

                // 如果 Prompt 内容包含参数占位符，进行参数替换
                if (!string.IsNullOrWhiteSpace(promptItem.VariableDictJson) &&
                    !string.IsNullOrWhiteSpace(promptItem.Prefix) &&
                    !string.IsNullOrWhiteSpace(promptItem.Suffix))
                {
                    var variableDict = (promptItem.VariableDictJson ?? "{}").GetObject<Dictionary<string, string>>()
                        ?? new Dictionary<string, string>();
                    foreach (var (key, value) in variableDict)
                    {
                        string placeholder = $"{promptItem.Prefix}{key}{promptItem.Suffix}";
                        completionPrompt = completionPrompt.Replace(placeholder, value ?? string.Empty);
                    }
                }
            }

            // 从数据库中获取模型信息
            var model = promptItem.AIModelDto;
            // 构建生成AI设置
            SenparcAiSetting aiSettings = _aiModelService.Value.BuildSenparcAiSetting(model);

            // 创建 AI Handler 处理器
            var handler = new AgentAiHandler(aiSettings);

            var chatOptions = _promptItemService.GetChatOptions(promptItem, completionPrompt);
            var iWantToRun = await handler.IWantTo()
                            .ConfigChatModel($"PromptResult_{promptResultId}",
                                            BuildChatClientAgentOptions(chatOptions))
                            .BuildKernelWithAgentSessionAsync();

            var agentSession = iWantToRun.Kernel.AgentSession
                ?? throw new NcfExceptionBase("AgentKernel 未创建 AgentSession，无法继续聊天");
            var chatMessageList = _promptResultChatService.GetChatMessageList(chatHistory);
            agentSession.SetInMemoryChatHistory(chatMessageList);

            // 调用 AI 接口
            var dt1 = SystemTime.Now;
            UsageDetails streamUsageDetails = null;
            void HandleStreamUpdate(AgentResponseUpdate update)
            {
                if (update == null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(update.Text) && onStreamEvent != null)
                {
                    onStreamEvent(new PromptResultStreamEvent
                    {
                        EventType = "chunk",
                        PromptResultId = promptResultId,
                        Text = update.Text,
                        IsFinal = false
                    });
                }

                if (update.Contents?.FirstOrDefault(z => z is UsageContent) is UsageContent usageContent
                    && usageContent.Details != null)
                {
                    streamUsageDetails = usageContent.Details;
                }
            }

            var aiResult = onStreamEvent == null
                ? await iWantToRun.RunChatAsync(userMessage, agentSession)
                : await iWantToRun.RunChatAsync(userMessage, agentSession, HandleStreamUpdate);
            var costTime = SystemTime.DiffTotalMS(dt1);
            var usageInfo = PromptUsageHelper.ResolveUsage(streamUsageDetails ?? TryGetUsageFromResult(aiResult));

            // 追加新的对话记录到 PromptResultChat
            var newChatMessages = new List<ChatMessageDto>
            {
                new ChatMessageDto { Role = "user", Content = userMessage },
                new ChatMessageDto { Role = "assistant", Content = aiResult.OutputString }
            };

            // 添加新的对话记录（会自动从现有最大序号+1开始）
            var newChatDtos = await _promptResultChatService.AddChatMessagesAsync(promptResultId, newChatMessages);

            promptResult.AppendUsageAndResult(
                usageInfo.PromptTokens,
                usageInfo.CompletionTokens,
                usageInfo.TotalTokens,
                costTime,
                aiResult.OutputString);
            await base.SaveObjectAsync(promptResult);

            if (onStreamEvent != null)
            {
                onStreamEvent(new PromptResultStreamEvent
                {
                    EventType = "final",
                    PromptResultId = promptResultId,
                    Text = aiResult.OutputString,
                    IsFinal = true,
                    PromptTokens = usageInfo.PromptTokens,
                    CompletionTokens = usageInfo.CompletionTokens,
                    TotalTokens = usageInfo.TotalTokens,
                    ResponseMilliseconds = (int)Math.Round(costTime)
                });
            }

            return newChatDtos;
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

        public async Task<PromptResult> RobotScoringAsync(int promptResultId, bool isRefresh, string expectedResultsJson)
        {
            List<string> list = expectedResultsJson.GetObject<List<string>>() ?? new List<string>();
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
            expectedResultList ??= new List<string>();
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

例如：
[期望结果]
Apple

[实际结果]
今天是星期天

[打分结果]
0


[期望结果]
Apple

[实际结果]
Apple

[打分结果]
10
";

            // 获取模型
            AIModelDto aiModelDto = null;
            //如果当前模型不是 Chat 类型，则需要找一个 Chat 模型
            if (promptItem.AIModelDto.ConfigModelType != ConfigModelType.Chat)
            {
                var chatModel = await _aiModelService.Value.GetObjectAsync(z => z.ConfigModelType == ConfigModelType.Chat);
                if (chatModel == null)
                {
                    throw new NcfExceptionBase("必须至少设置一个 Chat 类型的模型才能自动打分（在 AIKernel 模块中）");
                }
                aiModelDto = _aiModelService.Value.Mapping<AIModelDto>(chatModel);
            }
            else
            {
                aiModelDto = promptItem.AIModelDto;
            }

            // build aiSettings by model
            var aiSettings = _aiModelService.Value.BuildSenparcAiSetting(aiModelDto);
            //TODO:可以设置一个默k认 PromptRange 的值，或者使用配置来指定打分的 AI，而不是使用同一个模型。

            var expectedResult = expectedResultList.ToJson();

            var scoringChatOptions = new ChatOptions()
            {
                Instructions = scorePrompt,
                MaxOutputTokens = aiModelDto.MaxToken + expectedResult.Length + scorePrompt.Length + 500,
                Temperature = 0.2f,
                TopP = 0.2f,
                StopSequences = new List<string>()
            };

            var handler = new AgentAiHandler(aiSettings);
            var iWantToRun = await handler.IWantTo()
                .ConfigChatModel("AIScoring", BuildChatClientAgentOptions(scoringChatOptions))
                .BuildKernelWithAgentSessionAsync();

            var userPrompt = $@"
[期望结果]
{expectedResult}

[实际结果]
{promptResult.ResultString}

[打分结果]";

            var dt1 = SystemTime.Now;
            var scoringSession = iWantToRun.Kernel.AgentSession;
            var result = scoringSession == null
                ? await iWantToRun.RunChatAsync(userPrompt)
                : await iWantToRun.RunChatAsync(userPrompt, scoringSession);
            var resultOutput = result?.OutputString?.Trim() ?? string.Empty;
            SenparcTrace.SendCustomLog("自动打分结束", $"模型返回为{resultOutput}，花费时间{SystemTime.DiffTotalMS(dt1)}ms");

            // 正则匹配出 resultOutput 中的数字
            // Use regular expression to find matches

            // 匹配 MAX_SCORE，后面可以跟 0-2 位的小数
            string pattern = "^(10(\\.0{1,2})?|[1-9](\\.\\d{1,2})?|0(\\.\\d{1,2})?)$";
            //@"^100(\.[0-9]{1,2})?|([0-9]{1,2})(\.[0-9]{1,2})?$";
            //"^(100(\.0{1,2})?|[1-9]?\d(\.\d{1,2})?|0(\.\d{1,2})?)$"
            //^(10(\.0{1,2})?|[1-9](\.\d{1,2})?|0(\.\d{1,2})?)$
            Match match = Regex.Match(resultOutput, pattern);

            // If there is a match, the number will be match.Value
            if (!match.Success)
            {
                SenparcTrace.SendCustomLog("自动打分结束", $"原文为{resultOutput}，分数匹配失败");

                throw new NcfExceptionBase($"自动打分结果匹配失败, 被打分的结果字符串为：{promptResult.ResultString}, 模型返回为{resultOutput}，");
            }

            bool success = Decimal.TryParse(match.Value, out var score);
            if (!success)
            {
                throw new NcfExceptionBase($"自动打分结果匹配失败, 被打分的结果字符串为：{promptResult.ResultString}, 模型返回为{resultOutput}，");
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

        private static ChatClientAgentOptions BuildChatClientAgentOptions(ChatOptions chatOptions)
        {
            return new ChatClientAgentOptions
            {
                ChatOptions = chatOptions,
                ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions())
            };
        }

        private static List<ChatMessage> BuildChatMessagesFromDto(List<ChatMessageDto> chatHistory)
        {
            if (chatHistory == null || chatHistory.Count == 0)
            {
                return new List<ChatMessage>();
            }

            return chatHistory.Select(z => new ChatMessage(
                NormalizeChatRole(z.Role),
                z.Content ?? string.Empty)).ToList();
        }

        private static ChatRole NormalizeChatRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return ChatRole.User;
            }

            return role.Trim().ToLowerInvariant() switch
            {
                "assistant" => ChatRole.Assistant,
                "system" => ChatRole.System,
                _ => ChatRole.User
            };
        }

        private static UsageDetails TryGetUsageFromResult(SenparcAiResult aiResult)
        {
            if (aiResult == null)
            {
                return null;
            }

            var resultProperty = aiResult.GetType().GetProperty("Result");
            var resultObject = resultProperty?.GetValue(aiResult);
            if (resultObject == null)
            {
                return null;
            }

            var usageProperty = resultObject.GetType().GetProperty("Usage");
            return usageProperty?.GetValue(resultObject) as UsageDetails;
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
