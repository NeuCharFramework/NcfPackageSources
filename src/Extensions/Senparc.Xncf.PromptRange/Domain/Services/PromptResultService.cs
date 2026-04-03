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
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Senparc.CO2NET.Cache;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptResultService : ServiceBase<PromptResult>
    {
        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly LlModelService _llModelService;
        private readonly PromptResultChatService _promptResultChatService;

        public PromptResultService(
            IRepositoryBase<PromptResult> repo,
            IServiceProvider serviceProvider,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            LlModelService llModelService,
            PromptResultChatService promptResultChatService
            ) : base(repo,
            serviceProvider)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _llModelService = llModelService;
            _promptResultChatService = promptResultChatService;
        }


        public async Task<List<PromptResultDto>> GetByItemId(int promptItemId)
        {
            // var promptItem = await _promptItemService.GetObjectAsync(p => p.Id == promptItemId)
            //     ?? throw new NcfExceptionBase($"The prompt word corresponding to {promptItemId} was not found");

            var resultList = (await this.GetFullListAsync(
                p => p.PromptItemId == promptItemId,
                p => p.Id,
                OrderingType.Ascending));

            var dtoList = resultList
                .Select(p => this.Mapper.Map<PromptResultDto>(p))
                .ToList();

            return dtoList;
        }

        public async Task<PromptResultDto> SenparcGenerateResultAsync(PromptItemDto promptItem, string userMessage = null, List<ChatMessageDto> chatHistory = null)
        {
            //Define AI interface calling parameters and Token restrictions, etc.
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 200,
                Temperature = promptItem.Temperature,
                TopP = promptItem.TopP,
                FrequencyPenalty = promptItem.FrequencyPenalty,
                PresencePenalty = promptItem.PresencePenalty,
                StopSequences = (promptItem.StopSequences ?? "[]").GetObject<List<string>>(),
            };

            // You need to add $ before the variable
            //string completionPrompt = $@"Please output the corresponding content according to the prompts:
            //{promptItem.Content}";
            string completionPrompt = $@"{promptItem.Content}";
            
            // Generate SystemMessage after replacing parameters (for saving to database)
            // If the Prompt content contains parameter placeholders (such as {{$variableName}}), perform parameter substitution
            string systemMessage = completionPrompt;
            if (!string.IsNullOrWhiteSpace(promptItem.VariableDictJson) && 
                !string.IsNullOrWhiteSpace(promptItem.Prefix) && 
                !string.IsNullOrWhiteSpace(promptItem.Suffix))
            {
                // Read parameters and replace placeholders in Prompt content
                var variableDict = (promptItem.VariableDictJson ?? "{}").GetObject<Dictionary<string, string>>();
                foreach (var (key, value) in variableDict)
                {
                    // Replacement format: {Prefix}{key}{Suffix} -> value
                    // For example: {{$variableName}} -> actualValue
                    string placeholder = $"{promptItem.Prefix}{key}{promptItem.Suffix}";
                    systemMessage = systemMessage.Replace(placeholder, value ?? string.Empty);
                }
            }

            // Get model information from database
            var model = promptItem.AIModelDto;
            // Build to generate AI settings
            SenparcAiSetting aiSettings = this.BuildSenparcAiSetting(model);

            //TODO: model plus model type: Chat/TextCompletion/TextToImage, etc.
            ConfigModel configModel = _llModelService.ConvertToConfigModel(model.ConfigModelType);

            // Create AI Handler processor (can also be injected through factory dependency)
            var handler = new SemanticAiHandler(aiSettings);


            IWantToRun iWantToRun =
            userMessage != null
                ? handler.ChatConfig(promptParameter,
                                            userId: "Jeffrey",
                                            maxHistoryStore: 20,
                                            chatSystemMessage: completionPrompt,
                                            senparcAiSetting: aiSettings,
                                            kernelBuilderAction: kh =>
                                                { }
                                                )
                : handler.IWantTo(aiSettings)
                        // Replace todo with your real username, which may need to be obtained from NeuChar?
                        .ConfigModel(configModel, "SenparcGenerateResult")
                        .BuildKernel()
                        .CreateFunctionFromPrompt(completionPrompt, promptParameter)
                        .iWantToRun;

            var aiArguments = iWantToRun.CreateNewArguments().arguments;
            //aiArguments["input"] = promptItem.Content;

            #region 用户自定义参数

            if (!string.IsNullOrWhiteSpace(promptItem.VariableDictJson))
            {
                // If there are parameters, the suffix and suffix cannot be empty.
                if (string.IsNullOrWhiteSpace(promptItem.Prefix) || string.IsNullOrWhiteSpace(promptItem.Suffix))
                {
                    throw new NcfExceptionBase("前后缀不能为空");
                }

                // Read parameters and fill in
                foreach (var (k, v) in (promptItem.VariableDictJson ?? "{}").GetObject<Dictionary<string, string>>())
                {
                    // aiArguments[$"{promptItem.Prefix}{k}{promptItem.Suffix}"] = v;
                    aiArguments[k] = v;
                }
            }

            #endregion
            SenparcAiResult aiResult = null;
            var dt1 = SystemTime.Now;
            if (userMessage != null)
            {
                aiResult = await handler.ChatAsync(iWantToRun, userMessage);
            }
            else
            {
                var aiRequest = iWantToRun.CreateRequest(aiArguments, true);
                aiResult = await iWantToRun.RunAsync(aiRequest);
            }

            // todo calculates token consumption
            // Simple calculation
            // num_prompt_tokens = len(encoding.encode(string))
            // gap_between_send_receive = 15 * len(kwargs["messages"])
            // num_prompt_tokens += gap_between_send_receive
            var promptCostToken = 0;
            var resultCostToken = 0;

            // Determine whether it is chat mode
            var isChatMode = userMessage != null;
            var resultMode = isChatMode ? ResultMode.Chat : ResultMode.Single;

            // If in chat mode, save SystemMessage; otherwise null
            var promptResult = new PromptResult(
                promptItem.ModelId, aiResult.OutputString, SystemTime.DiffTotalMS(dt1),
                -1, -1, -1, TestType.Text,
                promptCostToken, resultCostToken, promptCostToken + resultCostToken,
                promptItem.FullVersion, promptItem.Id,
                resultMode,
                isChatMode ? systemMessage : null); // Save SystemMessage only in chat mode

            await base.SaveObjectAsync(promptResult);

            // If in chat mode, save the conversation record
            if (isChatMode && !string.IsNullOrWhiteSpace(userMessage) && !string.IsNullOrWhiteSpace(aiResult.OutputString))
            {
                var chatMessages = new List<ChatMessageDto>();

                // If there is a history record, add the history record first
                if (chatHistory != null && chatHistory.Count > 0)
                {
                    chatMessages.AddRange(chatHistory);
                }

                // Add current conversation (user messages and AI responses)
                chatMessages.Add(new ChatMessageDto { Role = "user", Content = userMessage });
                chatMessages.Add(new ChatMessageDto { Role = "assistant", Content = aiResult.OutputString });

                await _promptResultChatService.AddChatMessagesAsync(promptResult.Id, chatMessages);
            }

            // Have expected results and perform automatic scoring
            if (promptItem.isAIGrade && !string.IsNullOrWhiteSpace(promptItem.ExpectedResultsJson))
            {
                await this.RobotScoringAsync(promptResult.Id, false, promptItem.ExpectedResultsJson);
            }

            return this.Mapper.Map<PromptResultDto>(promptResult);
        }

        /// <summary>
        /// Continue chatting: Append the conversation record to the existing PromptResult without creating a new PromptResult
        /// </summary>
        /// <param name="promptResultId">Existing PromptResult ID</param>
        /// <param name="userMessage">User message</param>
        /// <returns>Returns newly added conversation records (user messages and AI replies)</returns>
        public async Task<List<PromptResultChatDto>> ContinueChatAsync(int promptResultId, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                throw new NcfExceptionBase("用户消息不能为空");
            }

            // Get existing PromptResult
            var promptResult = await this.GetObjectAsync(p => p.Id == promptResultId)
                ?? throw new NcfExceptionBase($"未找到 ID 为 {promptResultId} 的 PromptResult");

            // Verify whether it is in chat mode
            if (promptResult.Mode != ResultMode.Chat)
            {
                throw new NcfExceptionBase("只有聊天模式的 PromptResult 才能继续聊天");
            }

            // GetPromptItem
            var promptItem = await _promptItemService.GetAsync(promptResult.PromptItemId);

            // Get historical conversation records
            var chatHistory = await _promptResultChatService.GetByPromptResultIdAsync(promptResultId);
            // var chatHistoryForAI = chatHistory.Select(c => new ChatMessageDto
            // {
            //     Role = c.RoleType == ChatRoleType.User ? "user" : "assistant",
            //     Content = c.Content
            // }).ToList();

            // Define AI interface call parameters
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = promptItem.MaxToken > 0 ? promptItem.MaxToken : 200,
                Temperature = promptItem.Temperature,
                TopP = promptItem.TopP,
                FrequencyPenalty = promptItem.FrequencyPenalty,
                PresencePenalty = promptItem.PresencePenalty,
                StopSequences = (promptItem.StopSequences ?? "[]").GetObject<List<string>>(),
            };

            // The saved SystemMessage is used first, if not, the current Prompt content is used.
            // This ensures that even if the Prompt content or parameters change, the originally saved SystemMessage is used when continuing the conversation.
            string completionPrompt;
            if (!string.IsNullOrWhiteSpace(promptResult.SystemMessage))
            {
                // Use saved SystemMessage (parameter substitution done)
                completionPrompt = promptResult.SystemMessage;
            }
            else
            {
                // Downgrade scenario: If there is no saved SystemMessage, use the current Prompt content
                // This may happen with old data or data in Single mode
                completionPrompt = $@"{promptItem.Content}";
                
                // If the Prompt content contains parameter placeholders, perform parameter substitution
                if (!string.IsNullOrWhiteSpace(promptItem.VariableDictJson) && 
                    !string.IsNullOrWhiteSpace(promptItem.Prefix) && 
                    !string.IsNullOrWhiteSpace(promptItem.Suffix))
                {
                    var variableDict = (promptItem.VariableDictJson ?? "{}").GetObject<Dictionary<string, string>>();
                    foreach (var (key, value) in variableDict)
                    {
                        string placeholder = $"{promptItem.Prefix}{key}{promptItem.Suffix}";
                        completionPrompt = completionPrompt.Replace(placeholder, value ?? string.Empty);
                    }
                }
            }

            // Get model information from database
            var model = promptItem.AIModelDto;
            // Build to generate AI settings
            SenparcAiSetting aiSettings = this.BuildSenparcAiSetting(model);

            ConfigModel configModel = _llModelService.ConvertToConfigModel(model.ConfigModelType);

            // Create an AI Handler processor
            var handler = new SemanticAiHandler(aiSettings);

            var hisgoryArgName = "history";

            // Using ChatConfig, use a unique userId based on promptResultId
            IWantToRun iWantToRun = handler.ChatConfig(promptParameter,
                userId: $"PromptResult_{promptResultId}",
                maxHistoryStore: 20,
                chatSystemMessage: completionPrompt,
                senparcAiSetting: aiSettings,
                hisgoryArgName: hisgoryArgName,
                kernelBuilderAction: kh => { }
                );

            // Get history and add to KernelArguments
            var chatHistoryFromKernel = iWantToRun.StoredAiArguments.KernelArguments[hisgoryArgName] as ChatHistory;

            if (chatHistoryFromKernel != null)
            {
                foreach (var c in chatHistory)
                {
                    switch (c.RoleType)
                    {
                        case ChatRoleType.User:
                            chatHistoryFromKernel.AddUserMessage(c.Content);
                            break;
                        case ChatRoleType.Assistant:
                            chatHistoryFromKernel.AddAssistantMessage(c.Content);
                            break;
                    }
                }

                iWantToRun.StoredAiArguments.KernelArguments[hisgoryArgName] = chatHistoryFromKernel;
            }

            // Call AI interface
            var dt1 = SystemTime.Now;
            var aiResult = await handler.ChatAsync(iWantToRun, userMessage,historyArgName:hisgoryArgName);
            var costTime = SystemTime.DiffTotalMS(dt1);

            // Append new conversation records to PromptResultChat
            var newChatMessages = new List<ChatMessageDto>
            {
                new ChatMessageDto { Role = "user", Content = userMessage },
                new ChatMessageDto { Role = "assistant", Content = aiResult.OutputString }
            };

            // Add a new conversation record (it will automatically start from the existing maximum sequence number + 1)
            var newChatDtos = await _promptResultChatService.AddChatMessagesAsync(promptResultId, newChatMessages);

            // Update ResultString of PromptResult (append new reply)
            // Note: Since ResultString is a private set, we need to use reflection or add an update method
            // We will not update ResultString here because the conversation record has been saved in PromptResultChat.
            // If you need to update, you can add an UpdateResultString method

            return newChatDtos;
        }

        public async Task<PromptResult> ManualScoreAsync(int id, decimal score)
        {
            #region validate

            //Verify score >= 0
            if (score < 0)
            {
                throw new NcfExceptionBase("分数不能小于0");
            }

            #endregion

            // Search database based on id
            var promptResult = await base.GetObjectAsync(result => result.Id == id) ??
                               throw new NcfExceptionBase($"未找到{id}对应的结果");

            promptResult.ManualScoring(score);
            promptResult.FinalScoring(score);

            await base.SaveObjectAsync(promptResult);

            return promptResult;
        }


        /// <summary>
        /// Construct SenparcAiSetting, used in two places
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
                        NeuCharAIApiVersion = llModel.ApiVersion, // ApiVersion is not actually used in SK
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
                        AzureOpenAIApiVersion = llModel.ApiVersion, // ApiVersion is not actually used in SK
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
                        AzureOpenAIApiVersion = llModel.ApiVersion, // ApiVersion is not actually used in SK
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
                case AiPlatform.Ollama:
                    aiSettings.OllamaKeys = new OllamaKeys()
                    {
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
                case AiPlatform.DeepSeek:
                    aiSettings.DeepSeekKeys = new DeepSeekKeys()
                    {
                        ApiKey = llModel.ApiKey,
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
                default:
                    throw new NcfExceptionBase($"PromptRange 暂时不支持 {aiSettings.AiPlatform} 类型");
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
        ///AI scoring
        /// </summary>
        /// <param name="promptResultId"></param>
        /// <param name="isRefresh"></param>
        /// <param name="expectedResultList"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<PromptResult> RobotScoringAsync(int promptResultId, bool isRefresh, List<string> expectedResultList)
        {
            // You need to add $ before the variable
            const int MAX_SCORE = 10;
            const string MAX_SOCRE_STR = "10";


            // get promptResult by id
            var promptResult = await this.GetObjectAsync(z => z.Id == promptResultId)
                               ?? throw new NcfExceptionBase("找不到对应的promptResult");


            // get promptItem by promptResult.PromptItemId
            var promptItem = await _promptItemService.GetAsync(promptResult.PromptItemId)
                             ?? throw new NcfExceptionBase("找不到对应的promptItem");

            // Save a list of expected results
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

            //TODO: Add inequality sign rule



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


[期望结果]
{{{{$expectedResult}}}}

[实际结果]
{{{{$actualResult}}}}

[打分结果]
";

            // Get model
            var model = promptItem.AIModelDto;

            // build aiSettings by model
            var aiSettings = this.BuildSenparcAiSetting(model);
            //TODO: You can set a default PromptRange value, or use configuration to specify the scoring AI instead of using the same model.

            var expectedResult = expectedResultList.ToJson();

            //Define AI interface calling parameters and Token restrictions, etc.
            var promptParameter = new PromptConfigParameter()
            {
                MaxTokens = promptItem.AIModelDto.MaxToken + expectedResult.Length + scorePrompt.Length + 500,
                Temperature = 0.2,
                TopP = 0.2
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

            // Regularly match the numbers in result.Output
            // Use regular expression to find matches

            // Matches MAX_SCORE, which can be followed by 0-2 decimal places
            string pattern = "^(10(\\.0{1,2})?|[1-9](\\.\\d{1,2})?|0(\\.\\d{1,2})?)$";
            //@"^100(\.[0-9]{1,2})?|([0-9]{1,2})(\.[0-9]{1,2})?$";
            //"^(100(\.0{1,2})?|[1-9]?\d(\.\d{1,2})?|0(\.\d{1,2})?)$"
            //^(10(\.0{1,2})?|[1-9](\.\d{1,2})?|0(\.\d{1,2})?)$
            Match match = Regex.Match(result.Output, pattern);

            // If there is a match, the number will be match.Value
            if (!match.Success)
            {
                SenparcTrace.SendCustomLog("自动打分结束", $"原文为{result.Output}，分数匹配失败");

                throw new NcfExceptionBase($"自动打分结果匹配失败, 被打分的结果字符串为：{promptResult.ResultString}, 模型返回为{result.Output}，");
            }

            bool success = Decimal.TryParse(match.Value, out var score);
            if (!success)
            {
                throw new NcfExceptionBase($"自动打分结果匹配失败, 被打分的结果字符串为：{promptResult.ResultString}, 模型返回为{result.Output}，");
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
        /// Update the average score and maximum score of promptItem
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
                // no results
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