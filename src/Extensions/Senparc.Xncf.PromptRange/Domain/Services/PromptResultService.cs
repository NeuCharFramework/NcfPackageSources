/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptResultService.cs
    文件功能描述：PromptResultService 服务逻辑
    
    
    创建标识：Senparc - 20231021
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260705
    修改描述：v0.16.3-preview2 增强文生图重试机制并兼容 TLS1.2/TLS1.3

    修改标识：Senparc - 20260705
    修改描述：v0.16.4-preview3 增强文生图重试机制并兼容 TLS1.2/TLS1.3

    修改标识：Senparc - 20260707
    修改描述：v0.16.5-preview4 增强 Embedding 请求兼容处理并优化向量检索稳定性

    修改标识：Senparc - 20260715
    修改描述：v0.16.5-preview4 升级 Senparc.AI 至 0.27.3 与 Senparc.AI.AgentKernel 至 0.1.10

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI.Audio;
using OpenAI.Embeddings;
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
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptResultService : ServiceBase<PromptResult>
    {
        private const string TextToImageResultTypeTag = "TextToImage";
        private const string TextToSpeechResultTypeTag = "TextToSpeech";
        private const string SpeechToTextResultTypeTag = "SpeechToText";
        private const string TextEmbeddingResultTypeTag = "TextEmbedding";
        private const string TextToImageStorageTypeLocal = "LocalFile";
        private const string TextToImageStorageTypeRemote = "RemoteUrl";
        private const string TextToImageStorageTypeNone = "None";
        private const string PromptAudioStorageTypeLocal = "LocalFile";
        private const string PromptAudioStorageTypeNone = "None";
        private const int DefaultTextToImageWidth = 1024;
        private const int DefaultTextToImageHeight = 1024;
        private const int DefaultEmbeddingSearchTopK = 3;
        private const int MaxEmbeddingSearchTopK = 20;
        private static long _embeddingSourceCounter = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private static readonly JsonSerializerOptions PromptResultJsonSerializerOptions = new(JsonSerializerDefaults.Web);

        // private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly PromptRangeService _promptRangeService;
        private readonly LlModelService _llModelService;
        private readonly PromptResultChatService _promptResultChatService;
        private readonly Lazy<AIModelService> _aiModelService;
        private readonly Lazy<AIVectorService> _aiVectorService;

        public PromptResultService(
            IRepositoryBase<PromptResult> repo,
            IServiceProvider serviceProvider,
            PromptItemService promptItemService,
            PromptRangeService promptRangeService,
            LlModelService llModelService,
            PromptResultChatService promptResultChatService,
            Lazy<AIModelService> aiModelService,
            Lazy<AIVectorService> aiVectorService) : base(repo,
            serviceProvider)
        {
            _promptItemService = promptItemService;
            _promptRangeService = promptRangeService;
            _llModelService = llModelService;
            _promptResultChatService = promptResultChatService;
            _aiModelService = aiModelService;
            _aiVectorService = aiVectorService;
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
            Action<PromptResultStreamEvent> onStreamEvent = null,
            PromptExecutionOptions executionOptions = null)
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

            #region 用户自定义参数

            var history = BuildChatMessagesFromDto(chatHistory);

            if (!string.IsNullOrWhiteSpace(promptItem.VariableDictJson))
            {
                // 如果有参数，前后缀不能为空
                if (string.IsNullOrWhiteSpace(promptItem.Prefix) || string.IsNullOrWhiteSpace(promptItem.Suffix))
                {
                    throw new NcfExceptionBase("前后缀不能为空");
                }
            }

            #endregion
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

            switch (model.ConfigModelType)
            {
                case ConfigModelType.TextToImage:
                    return await ExecuteTextToImageAsync(
                        promptItem,
                        model,
                        runPrompt,
                        onStreamEvent);
                case ConfigModelType.TextToSpeech:
                    return await ExecuteTextToSpeechAsync(
                        promptItem,
                        model,
                        runPrompt,
                        executionOptions,
                        onStreamEvent);
                case ConfigModelType.SpeechToText:
                case ConfigModelType.SpeechRecognition:
                    return await ExecuteSpeechToTextAsync(
                        promptItem,
                        model,
                        executionOptions,
                        onStreamEvent);
                case ConfigModelType.TextEmbedding:
                    return await ExecuteTextEmbeddingAsync(
                        promptItem,
                        model,
                        runPrompt,
                        executionOptions,
                        onStreamEvent);
                case ConfigModelType.Chat:
                    break;
                default:
                    throw new NcfExceptionBase(
                        $"PromptRange 当前暂不支持 {model.ConfigModelType} 类型模型打靶。已支持：Chat、TextToImage、TextToSpeech、SpeechToText、TextEmbedding。");
            }

            var generateScene = $"PromptRange.Generate#{CreateTraceToken()}";
            var (aiOutput, usageFromResponse) = await ExecuteChatWithDefaultFallbackAsync(
                model,
                promptParameter,
                $"PromptRange_{promptItem.Id}",
                runPrompt,
                history,
                generateScene,
                onStreamEvent == null ? null : HandleStreamUpdate);
            var usageInfo = PromptUsageHelper.ResolveUsage(streamUsageDetails ?? usageFromResponse);
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

        private async Task<PromptResultDto> ExecuteTextToImageAsync(
            PromptItemDto promptItem,
            AIModelDto model,
            string runPrompt,
            Action<PromptResultStreamEvent> onStreamEvent)
        {
            var textToImageScene = $"PromptRange.TextToImage#{CreateTraceToken()}";
            var dt1 = SystemTime.Now;
            PromptTextToImageResultPayload payload;

            try
            {
                payload = await GenerateTextToImagePayloadViaAgentKernelAsync(
                    promptItem,
                    model,
                    runPrompt,
                    textToImageScene);
            }
            catch (NcfExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw BuildAiCallException(ex, model, textToImageScene);
            }

            if (string.IsNullOrWhiteSpace(payload.ImageUrl))
            {
                throw new NcfExceptionBase("图像生成成功，但未返回可访问的图片地址（ImageBytes / ImageUri 均为空）");
            }

            var promptCostToken = EstimateTokenCount(runPrompt);
            var resultCostToken = EstimateTokenCount(payload.RevisedPrompt);
            var totalCostToken = promptCostToken + resultCostToken;
            var costTime = SystemTime.DiffTotalMS(dt1);
            var resultString = JsonSerializer.Serialize(payload, PromptResultJsonSerializerOptions);

            var promptResult = new PromptResult(
                promptItem.ModelId,
                resultString,
                costTime,
                -1,
                -1,
                -1,
                TestType.Graph,
                promptCostToken,
                resultCostToken,
                totalCostToken,
                promptItem.FullVersion,
                promptItem.Id,
                ResultMode.Single,
                null);

            await base.SaveObjectAsync(promptResult);

            SenparcTrace.SendCustomLog(
                "PromptRange.AI.Attempt",
                $"{textToImageScene} 请求成功。PromptTokens={promptCostToken}, CompletionTokens={resultCostToken}, TotalTokens={totalCostToken}, Storage={payload.StorageType}");

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
                    Text = resultString,
                    IsFinal = true,
                    PromptTokens = promptCostToken,
                    CompletionTokens = resultCostToken,
                    TotalTokens = totalCostToken,
                    ResponseMilliseconds = (int)Math.Round(costTime)
                });
            }

            return this.Mapper.Map<PromptResultDto>(promptResult);
        }

        private async Task<PromptTextToImageResultPayload> GenerateTextToImagePayloadViaAgentKernelAsync(
            PromptItemDto promptItem,
            AIModelDto model,
            string prompt,
            string scene)
        {
            try
            {
                return await GenerateTextToImagePayloadViaAgentKernelCoreAsync(
                    promptItem,
                    model,
                    prompt,
                    scene,
                    null);
            }
            catch (Exception ex) when (ShouldRetryTextToImageWithCompatClient(model, ex))
            {
                SenparcTrace.SendCustomLog(
                    "PromptRange.AI.Retry",
                    $"{scene} 检测到 SSL/传输层连接重置，使用兼容 HttpClient（HTTP/1.1 + TLS12/13）重试一次。Model={BuildModelDiagnosticInfo(model)}");

                using var compatibilityHttpClient = CreateTextToImageCompatibilityHttpClient();
                return await GenerateTextToImagePayloadViaAgentKernelCoreAsync(
                    promptItem,
                    model,
                    prompt,
                    scene,
                    compatibilityHttpClient);
            }
        }

        private async Task<PromptTextToImageResultPayload> GenerateTextToImagePayloadViaAgentKernelCoreAsync(
            PromptItemDto promptItem,
            AIModelDto model,
            string prompt,
            string scene,
            HttpClient httpClient)
        {
            // TextToImage 使用当前模型类型对应的最小模型配置，避免混入 Chat/Completion 模型字段。
            var aiSettings = _aiModelService.Value.BuildSenparcAiSetting(model, null);
            var runnerName = $"PromptRange_{promptItem.Id}";
            SenparcTrace.SendCustomLog(
                "PromptRange.AI.Attempt",
                $"{scene} 开始请求。Runner={runnerName}, Model={BuildModelDiagnosticInfo(model)}");

            var handler = httpClient == null
                ? new AgentAiHandler(aiSettings)
                : new AgentAiHandler(aiSettings, null, null, httpClient, false);

            var iWantToRun = handler
                .IWantTo(aiSettings)
                .ConfigImageModel(runnerName)
                .BuildKernel();

            var kernel = iWantToRun?.Kernel
                ?? throw new NcfExceptionBase("TextToImage 调用失败：Kernel 未创建成功");

            var imageResult = await kernel.ImageGenerationAsync(
                prompt,
                DefaultTextToImageWidth,
                DefaultTextToImageHeight);

            var generatedImage = imageResult?.Value
                ?? throw BuildAiCallException(
                    new Exception("ImageGenerationAsync 未返回有效结果（Value 为 null）"),
                    model,
                    scene);

            return await BuildTextToImagePayloadAsync(
                prompt,
                generatedImage.RevisedPrompt,
                generatedImage.ImageBytes,
                generatedImage.ImageUri);
        }

        private static bool ShouldRetryTextToImageWithCompatClient(AIModelDto model, Exception ex)
        {
            if (model == null || ex == null)
            {
                return false;
            }

            if (model.AiPlatform != AiPlatform.AzureOpenAI && model.AiPlatform != AiPlatform.NeuCharAI)
            {
                return false;
            }

            return ContainsSslTransportError(ex);
        }

        private static HttpClient CreateTextToImageCompatibilityHttpClient()
        {
            var socketsHandler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                ConnectTimeout = TimeSpan.FromSeconds(20),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                SslOptions = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                }
            };

            return new HttpClient(socketsHandler)
            {
                Timeout = TimeSpan.FromSeconds(120),
                DefaultRequestVersion = HttpVersion.Version11,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };
        }

        private async Task<PromptResultDto> ExecuteTextToSpeechAsync(
            PromptItemDto promptItem,
            AIModelDto model,
            string runPrompt,
            PromptExecutionOptions executionOptions,
            Action<PromptResultStreamEvent> onStreamEvent)
        {
            var scene = $"PromptRange.TextToSpeech#{CreateTraceToken()}";
            var dt1 = SystemTime.Now;

            var voice = NormalizeTextToSpeechVoice(executionOptions?.TextToSpeech?.Voice);
            var format = NormalizeTextToSpeechFormat(executionOptions?.TextToSpeech?.Format);
            var speedRatio = NormalizeTextToSpeechSpeed(executionOptions?.TextToSpeech?.SpeedRatio);

            PromptTextToSpeechResultPayload payload;
            try
            {
                var aiSettings = _aiModelService.Value.BuildSenparcAiSetting(model, null);
                var runnerName = $"PromptRange_{promptItem.Id}";
                SenparcTrace.SendCustomLog(
                    "PromptRange.AI.Attempt",
                    $"{scene} 开始请求。Runner={runnerName}, Model={BuildModelDiagnosticInfo(model)}, Voice={voice}, Format={format}, Speed={speedRatio}");

                var iWantToRun = new AgentAiHandler(aiSettings)
                    .IWantTo(aiSettings)
                    .ConfigTextToSpeechModel(runnerName)
                    .BuildKernel();

                var audioData = await iWantToRun.RunTextToSpeechAsync(runPrompt, voice, format, speedRatio: speedRatio);
                var audioBytes = audioData?.ToArray();
                if (audioBytes == null || audioBytes.Length == 0)
                {
                    throw new NcfExceptionBase("TTS 调用成功，但未返回有效音频内容");
                }

                var extension = NormalizeAudioExtensionByFormat(format);
                var localRelativePath = await SavePromptAudioToAppDataAsync(
                    audioBytes,
                    "TextToSpeech",
                    extension,
                    "tts");

                payload = new PromptTextToSpeechResultPayload
                {
                    Text = runPrompt,
                    Voice = voice,
                    Format = format,
                    SpeedRatio = speedRatio,
                    LocalRelativePath = localRelativePath,
                    AudioUrl = BuildPromptAudioApiUrl(localRelativePath),
                    ContentType = DetectAudioContentType(extension),
                    StorageType = PromptAudioStorageTypeLocal,
                    AudioBytesLength = audioBytes.LongLength
                };
            }
            catch (NcfExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw BuildAiCallException(ex, model, scene);
            }

            var promptCostToken = EstimateTokenCount(runPrompt);
            var resultCostToken = EstimateTokenCount(runPrompt);
            var totalCostToken = promptCostToken + resultCostToken;
            var costTime = SystemTime.DiffTotalMS(dt1);
            var resultString = JsonSerializer.Serialize(payload, PromptResultJsonSerializerOptions);

            var promptResult = new PromptResult(
                promptItem.ModelId,
                resultString,
                costTime,
                -1,
                -1,
                -1,
                TestType.Voice,
                promptCostToken,
                resultCostToken,
                totalCostToken,
                promptItem.FullVersion,
                promptItem.Id,
                ResultMode.Single,
                null);

            await base.SaveObjectAsync(promptResult);

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
                    Text = resultString,
                    IsFinal = true,
                    PromptTokens = promptCostToken,
                    CompletionTokens = resultCostToken,
                    TotalTokens = totalCostToken,
                    ResponseMilliseconds = (int)Math.Round(costTime)
                });
            }

            return this.Mapper.Map<PromptResultDto>(promptResult);
        }

        private async Task<PromptResultDto> ExecuteSpeechToTextAsync(
            PromptItemDto promptItem,
            AIModelDto model,
            PromptExecutionOptions executionOptions,
            Action<PromptResultStreamEvent> onStreamEvent)
        {
            var scene = $"PromptRange.SpeechToText#{CreateTraceToken()}";
            var dt1 = SystemTime.Now;

            var sttOptions = executionOptions?.SpeechToText;
            var (audioPath, audioRelativePath, audioFileName, audioContentType, audioBytesLength) =
                await ResolveSpeechToTextAudioAsync(sttOptions);

            PromptSpeechToTextResultPayload payload;
            try
            {
                var aiSettings = _aiModelService.Value.BuildSenparcAiSetting(model, null);
                var runnerName = $"PromptRange_{promptItem.Id}";
                SenparcTrace.SendCustomLog(
                    "PromptRange.AI.Attempt",
                    $"{scene} 开始请求。Runner={runnerName}, Model={BuildModelDiagnosticInfo(model)}, Audio={audioFileName}");

                var iWantToRun = new AgentAiHandler(aiSettings)
                    .IWantTo(aiSettings)
                    .ConfigSpeechToTextModel(runnerName)
                    .BuildKernel();

                var options = new AudioTranscriptionOptions
                {
                    Language = string.IsNullOrWhiteSpace(sttOptions?.Language) ? null : sttOptions.Language.Trim()
                };

                var sttResult = await iWantToRun.Kernel.SpeechToTextAsync(audioPath, options);
                var transcript = sttResult?.Value?.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    throw new NcfExceptionBase("STT 调用成功，但未返回识别文本");
                }

                payload = new PromptSpeechToTextResultPayload
                {
                    Transcript = transcript,
                    Language = sttResult?.Value?.Language ?? options.Language ?? string.Empty,
                    Duration = sttResult?.Value?.Duration?.ToString(),
                    SourceAudioFileName = audioFileName,
                    SourceAudioLocalRelativePath = audioRelativePath,
                    SourceAudioUrl = BuildPromptAudioApiUrl(audioRelativePath),
                    SourceAudioContentType = audioContentType,
                    SourceAudioBytesLength = audioBytesLength,
                    TokenStatSource = "Estimated"
                };
            }
            catch (NcfExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw BuildAiCallException(ex, model, scene);
            }

            var promptCostToken = 0;
            var resultCostToken = EstimateTokenCount(payload.Transcript);
            var totalCostToken = promptCostToken + resultCostToken;
            var costTime = SystemTime.DiffTotalMS(dt1);
            var resultString = JsonSerializer.Serialize(payload, PromptResultJsonSerializerOptions);

            var promptResult = new PromptResult(
                promptItem.ModelId,
                resultString,
                costTime,
                -1,
                -1,
                -1,
                TestType.Voice,
                promptCostToken,
                resultCostToken,
                totalCostToken,
                promptItem.FullVersion,
                promptItem.Id,
                ResultMode.Single,
                null);

            await base.SaveObjectAsync(promptResult);

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
                    Text = resultString,
                    IsFinal = true,
                    PromptTokens = promptCostToken,
                    CompletionTokens = resultCostToken,
                    TotalTokens = totalCostToken,
                    ResponseMilliseconds = (int)Math.Round(costTime)
                });
            }

            return this.Mapper.Map<PromptResultDto>(promptResult);
        }

        private async Task<PromptResultDto> ExecuteTextEmbeddingAsync(
            PromptItemDto promptItem,
            AIModelDto model,
            string runPrompt,
            PromptExecutionOptions executionOptions,
            Action<PromptResultStreamEvent> onStreamEvent)
        {
            var scene = $"PromptRange.TextEmbedding#{CreateTraceToken()}";
            var dt1 = SystemTime.Now;

            if (string.IsNullOrWhiteSpace(runPrompt))
            {
                throw new NcfExceptionBase("TextEmbedding 执行内容不能为空");
            }

            var embeddingOptions = executionOptions?.TextEmbedding ?? new PromptTextEmbeddingExecutionOptions();
            var vectorDbId = embeddingOptions.VectorDbId ?? 0;
            if (vectorDbId <= 0)
            {
                throw new NcfExceptionBase("TextEmbedding 需要先选择 VectorDB");
            }

            var vectorDb = await _aiVectorService.Value.GetObjectAsync(z => z.Id == vectorDbId)
                ?? throw new NcfExceptionBase($"未找到 VectorDB（ID={vectorDbId}）");
            var vectorDto = _aiVectorService.Value.Mapper.Map<AIVectorDto>(vectorDb);

            var collectionName = BuildEmbeddingCollectionName(embeddingOptions.CollectionName, promptItem, model);
            var searchTopK = NormalizeEmbeddingSearchTopK(embeddingOptions.SearchTopK);
            var searchQuery = embeddingOptions.SearchQuery?.Trim();

            PromptTextEmbeddingResultPayload payload;
            try
            {
                var aiSettings = _aiModelService.Value.BuildSenparcAiSetting(model, vectorDto);
                var runnerName = $"PromptRange_{promptItem.Id}";
                SenparcTrace.SendCustomLog(
                    "PromptRange.AI.Attempt",
                    $"{scene} 开始请求。Runner={runnerName}, Model={BuildModelDiagnosticInfo(model)}, VectorDb={vectorDto.Alias}, Collection={collectionName}");

                var iWantToRun = new AgentAiHandler(aiSettings)
                    .IWantTo(aiSettings)
                    .ConfigTextEmbeddingModel(runnerName, collectionName)
                    .BuildKernel();
                ApplyEmbeddingRequestCompatibility(iWantToRun, model, scene);

                var store = iWantToRun.CreateTextSearchStore();

                var sourceId = CreateEmbeddingSourceId();
                var sourceName = $"{promptItem.FullVersion}-doc-{sourceId}";
                await store.UpsertDocumentsAsync(new List<TextSearchDocument>
                {
                    new TextSearchDocument
                    {
                        SourceId = sourceId,
                        SourceName = sourceName,
                        SourceLink = $"PromptRange://{promptItem.FullVersion}",
                        Text = runPrompt
                    }
                });

                var embeddingVector = await iWantToRun.GetEmbeddingAsync(runPrompt);
                var searchHits = new List<PromptTextEmbeddingSearchHitPayload>();
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var hits = await store.SearchAsync(searchQuery, searchTopK);
                    searchHits = MapTextEmbeddingSearchHits(hits);
                }

                payload = new PromptTextEmbeddingResultPayload
                {
                    VectorDbId = vectorDto.Id,
                    VectorDbAlias = vectorDto.Alias,
                    VectorDbType = vectorDto.VectorDBType.ToString(),
                    CollectionName = collectionName,
                    SourceText = runPrompt,
                    SourceTextLength = runPrompt.Length,
                    UpsertSourceId = sourceId.ToString(),
                    UpsertSourceName = sourceName,
                    EmbeddingDimension = embeddingVector.Length,
                    SearchQuery = searchQuery,
                    SearchTopK = searchTopK,
                    SearchHits = searchHits,
                    TokenStatSource = "Estimated"
                };
            }
            catch (NcfExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw BuildAiCallException(ex, model, scene);
            }

            var promptCostToken = EstimateTokenCount(runPrompt);
            var resultCostToken = EstimateTokenCount(searchQuery) + payload.SearchHits.Count;
            var totalCostToken = promptCostToken + resultCostToken;
            var costTime = SystemTime.DiffTotalMS(dt1);
            var resultString = JsonSerializer.Serialize(payload, PromptResultJsonSerializerOptions);

            var promptResult = new PromptResult(
                promptItem.ModelId,
                resultString,
                costTime,
                -1,
                -1,
                -1,
                TestType.Text,
                promptCostToken,
                resultCostToken,
                totalCostToken,
                promptItem.FullVersion,
                promptItem.Id,
                ResultMode.Single,
                null);

            await base.SaveObjectAsync(promptResult);

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
                    Text = resultString,
                    IsFinal = true,
                    PromptTokens = promptCostToken,
                    CompletionTokens = resultCostToken,
                    TotalTokens = totalCostToken,
                    ResponseMilliseconds = (int)Math.Round(costTime)
                });
            }

            return this.Mapper.Map<PromptResultDto>(promptResult);
        }

        public async Task<PromptResult_TextEmbeddingSearchResponse> TextEmbeddingSearchAsync(
            int promptResultId,
            string query,
            int topK)
        {
            if (promptResultId <= 0)
            {
                throw new NcfExceptionBase("PromptResultId 不能为空");
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new NcfExceptionBase("查询内容不能为空");
            }

            var promptResult = await this.GetObjectAsync(z => z.Id == promptResultId)
                ?? throw new NcfExceptionBase($"未找到 PromptResult（ID={promptResultId}）");

            if (!TryParseTextEmbeddingPayload(promptResult.ResultString, out var embeddingPayload))
            {
                throw new NcfExceptionBase("当前结果不是 TextEmbedding 类型，无法执行查询测试");
            }

            if (embeddingPayload.VectorDbId <= 0)
            {
                throw new NcfExceptionBase("当前 TextEmbedding 结果缺少 VectorDB 信息");
            }

            var vectorDb = await _aiVectorService.Value.GetObjectAsync(z => z.Id == embeddingPayload.VectorDbId)
                ?? throw new NcfExceptionBase($"未找到 VectorDB（ID={embeddingPayload.VectorDbId}）");
            var vectorDto = _aiVectorService.Value.Mapper.Map<AIVectorDto>(vectorDb);

            var promptItem = await _promptItemService.GetAsync(promptResult.PromptItemId);
            var model = promptItem.AIModelDto
                ?? throw new NcfExceptionBase($"找不到 PromptItem（{promptItem.FullVersion}）对应的 AI 模型配置");

            var normalizedTopK = NormalizeEmbeddingSearchTopK(topK > 0 ? topK : embeddingPayload.SearchTopK);
            var normalizedQuery = query.Trim();
            var scene = $"PromptRange.TextEmbeddingSearch#{CreateTraceToken()}";
            if (IsInMemoryVectorDbType(vectorDto.VectorDBType))
            {
                await EnsureTextEmbeddingSourceForInMemorySearchAsync(
                    model,
                    vectorDto,
                    embeddingPayload,
                    promptResultId,
                    scene);
            }
            var searchHits = await SearchTextEmbeddingStoreAsync(
                model,
                vectorDto,
                embeddingPayload.CollectionName,
                normalizedQuery,
                normalizedTopK,
                scene);

            return new PromptResult_TextEmbeddingSearchResponse
            {
                PromptResultId = promptResultId,
                VectorDbId = vectorDto.Id,
                VectorDbAlias = vectorDto.Alias,
                CollectionName = embeddingPayload.CollectionName,
                Query = normalizedQuery,
                TopK = normalizedTopK,
                Hits = searchHits.Select(z => new PromptResult_TextEmbeddingSearchHitResponse
                {
                    SourceId = z.SourceId,
                    SourceName = z.SourceName,
                    SourceLink = z.SourceLink,
                    Text = z.Text,
                    Score = z.Score
                }).ToList()
            };
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
            var model = promptItem.AIModelDto
                ?? throw new NcfExceptionBase($"找不到 PromptItem（{promptItem.FullVersion}）对应的 AI 模型配置");

            var chatOptions = _promptItemService.GetChatOptions(promptItem, completionPrompt);
            var chatMessageList = _promptResultChatService.GetChatMessageList(chatHistory);

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

            var continueScene = $"PromptRange.ContinueChat#{CreateTraceToken()}";
            var (aiOutput, usageFromResponse) = await ExecuteChatWithDefaultFallbackAsync(
                model,
                chatOptions,
                $"PromptResult_{promptResultId}",
                userMessage,
                chatMessageList,
                continueScene,
                onStreamEvent == null ? null : HandleStreamUpdate);
            var costTime = SystemTime.DiffTotalMS(dt1);
            var usageInfo = PromptUsageHelper.ResolveUsage(streamUsageDetails ?? usageFromResponse);

            // 追加新的对话记录到 PromptResultChat
            var newChatMessages = new List<ChatMessageDto>
            {
                new ChatMessageDto { Role = "user", Content = userMessage },
                new ChatMessageDto { Role = "assistant", Content = aiOutput }
            };

            // 添加新的对话记录（会自动从现有最大序号+1开始）
            var newChatDtos = await _promptResultChatService.AddChatMessagesAsync(promptResultId, newChatMessages);

            promptResult.AppendUsageAndResult(
                usageInfo.PromptTokens,
                usageInfo.CompletionTokens,
                usageInfo.TotalTokens,
                costTime,
                aiOutput);
            await base.SaveObjectAsync(promptResult);

            if (onStreamEvent != null)
            {
                onStreamEvent(new PromptResultStreamEvent
                {
                    EventType = "final",
                    PromptResultId = promptResultId,
                    Text = aiOutput,
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

            var scoringActualResult = GetResultTextForScoring(promptResult);

            // check if matching expected results
            // if matched,score 10 by default save promptResult and return
            var isMatch = expectedResultList.Any(r => r == scoringActualResult || (r.StartsWith("=") && r.Substring(1, r.Length - 1) == scoringActualResult));
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

            var expectedResult = expectedResultList.ToJson();

            var scoringChatOptions = new ChatOptions()
            {
                Instructions = scorePrompt,
                MaxOutputTokens = aiModelDto.MaxToken + expectedResult.Length + scorePrompt.Length + 500,
                Temperature = 0.2f,
                TopP = 0.2f,
                StopSequences = new List<string>()
            };

            var userPrompt = $@"
[期望结果]
{expectedResult}

[实际结果]
{scoringActualResult}

[打分结果]";

            var dt1 = SystemTime.Now;
            var scoringScene = $"PromptRange.RobotScoring#{CreateTraceToken()}";
            var (scoreOutput, _) = await ExecuteChatWithDefaultFallbackAsync(
                aiModelDto,
                scoringChatOptions,
                "AIScoring",
                userPrompt,
                null,
                scoringScene);
            var resultOutput = scoreOutput?.Trim() ?? string.Empty;
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

                throw new NcfExceptionBase($"自动打分结果匹配失败, 被打分的结果字符串为：{scoringActualResult}, 模型返回为{resultOutput}，");
            }

            bool success = Decimal.TryParse(match.Value, out var score);
            if (!success)
            {
                throw new NcfExceptionBase($"自动打分结果匹配失败, 被打分的结果字符串为：{scoringActualResult}, 模型返回为{resultOutput}，");
            }

            #region error 打分结果不在 0-MAX_SCORE 之间

            score = score > MAX_SCORE ? MAX_SCORE : (score < 0 ? 0 : score);

            #endregion

            promptResult.RobotScoring(score);
            promptResult.FinalScoring(promptResult.RobotScore);

            await this.SaveObjectAsync(promptResult);

            return promptResult;
        }

        private async Task<(string Output, UsageDetails Usage)> ExecuteChatWithDefaultFallbackAsync(
            AIModelDto model,
            ChatOptions chatOptions,
            string runnerName,
            string prompt,
            List<ChatMessage> history,
            string scene,
            Action<AgentResponseUpdate> inStreamItemProceessing = null)
        {
            if (model == null)
            {
                throw new NcfExceptionBase($"AI 调用失败（{scene}）：模型配置为空（AIModelDto == null）");
            }

            try
            {
                return await ExecuteChatCoreAsync(
                    model,
                    chatOptions,
                    runnerName,
                    prompt,
                    history,
                    scene,
                    inStreamItemProceessing);
            }
            catch (Exception ex) when (ContainsForbiddenStatus(ex))
            {
                var primaryException = ex as NcfExceptionBase ?? BuildAiCallException(ex, model, scene);
                Exception deploymentFallbackError = null;
                var deploymentFallbackDetail = string.Empty;
                if (TryBuildAlternateDeploymentModel(model, out var deploymentFallbackModel))
                {
                    deploymentFallbackDetail = BuildModelDiagnosticInfo(deploymentFallbackModel);
                    SenparcTrace.SendCustomLog(
                        "PromptRange.AI.DeploymentFallback",
                        $"{scene} 主模型返回 403，尝试使用 ModelId 作为 DeploymentName 重试。Primary={BuildModelDiagnosticInfo(model)}; Fallback={deploymentFallbackDetail}");

                    try
                    {
                        var deploymentResult = await ExecuteChatCoreAsync(
                            deploymentFallbackModel,
                            chatOptions,
                            $"{runnerName}_Deployment",
                            prompt,
                            history,
                            $"{scene}.DeploymentFallback",
                            inStreamItemProceessing);
                        SenparcTrace.SendCustomLog(
                            "PromptRange.AI.DeploymentFallback",
                            $"{scene} Deployment 回退成功，已使用 ModelId 作为 DeploymentName 完成本次请求。");
                        return deploymentResult;
                    }
                    catch (Exception deploymentEx)
                    {
                        deploymentFallbackError = deploymentEx;
                        SenparcTrace.SendCustomLog(
                            "PromptRange.AI.DeploymentFallback",
                            $"{scene} Deployment 回退失败：{FlattenExceptionMessages(deploymentEx)}");
                    }
                }

                var chatCandidateHint = await BuildChatModelCandidatesDiagnosticAsync(model.Id);
                var defaultSetting = Senparc.AI.Config.SenparcAiSetting as SenparcAiSetting;
                if (!CanUseDefaultChatFallback(defaultSetting))
                {
                    if (deploymentFallbackError != null)
                    {
                        throw new NcfExceptionBase(
                            $"{primaryException.Message}{Environment.NewLine}部署名称回退配置：{deploymentFallbackDetail}{Environment.NewLine}部署名称回退重试失败：{FlattenExceptionMessages(deploymentFallbackError)}{Environment.NewLine}可用 Chat 模型候选：{chatCandidateHint}",
                            deploymentFallbackError);
                    }

                    throw new NcfExceptionBase(
                        $"{primaryException.Message}{Environment.NewLine}可用 Chat 模型候选：{chatCandidateHint}",
                        primaryException);
                }

                var defaultModel = BuildModelDtoFromDefaultSetting(defaultSetting);
                var defaultFallbackDetail = BuildModelDiagnosticInfo(defaultModel);
                if (defaultModel == null || IsSameChatConfig(model, defaultModel))
                {
                    if (deploymentFallbackError != null)
                    {
                        throw new NcfExceptionBase(
                            $"{primaryException.Message}{Environment.NewLine}部署名称回退配置：{deploymentFallbackDetail}{Environment.NewLine}部署名称回退重试失败：{FlattenExceptionMessages(deploymentFallbackError)}{Environment.NewLine}可用 Chat 模型候选：{chatCandidateHint}",
                            deploymentFallbackError);
                    }

                    throw new NcfExceptionBase(
                        $"{primaryException.Message}{Environment.NewLine}可用 Chat 模型候选：{chatCandidateHint}",
                        primaryException);
                }

                SenparcTrace.SendCustomLog(
                    "PromptRange.AI.DefaultFallback",
                    $"{scene} 主模型返回 403，尝试使用系统默认聊天配置重试。Primary={BuildModelDiagnosticInfo(model)}; Default={defaultFallbackDetail}");

                try
                {
                    var defaultResult = await ExecuteChatCoreAsync(
                        defaultModel,
                        chatOptions,
                        $"{runnerName}_Default",
                        prompt,
                        history,
                        $"{scene}.DefaultFallback",
                        inStreamItemProceessing,
                        defaultSetting);
                    SenparcTrace.SendCustomLog(
                        "PromptRange.AI.DefaultFallback",
                        $"{scene} 默认配置回退成功，已使用系统默认聊天配置完成本次请求。");
                    return defaultResult;
                }
                catch (Exception fallbackEx)
                {
                    var messageBuilder = new StringBuilder();
                    messageBuilder.Append(primaryException.Message);
                    if (deploymentFallbackError != null)
                    {
                        messageBuilder.AppendLine();
                        messageBuilder.Append("部署名称回退配置：");
                        messageBuilder.Append(deploymentFallbackDetail);
                        messageBuilder.AppendLine();
                        messageBuilder.Append("部署名称回退重试失败：");
                        messageBuilder.Append(FlattenExceptionMessages(deploymentFallbackError));
                    }
                    messageBuilder.AppendLine();
                    messageBuilder.Append("系统默认回退配置：");
                    messageBuilder.Append(defaultFallbackDetail);
                    messageBuilder.AppendLine();
                    messageBuilder.Append("系统默认聊天配置重试失败：");
                    messageBuilder.Append(FlattenExceptionMessages(fallbackEx));
                    messageBuilder.AppendLine();
                    messageBuilder.Append("可用 Chat 模型候选：");
                    messageBuilder.Append(chatCandidateHint);
                    throw new NcfExceptionBase(messageBuilder.ToString(), fallbackEx);
                }
            }
        }

        private async Task<string> BuildChatModelCandidatesDiagnosticAsync(int excludeModelId)
        {
            try
            {
                var chatModels = await _aiModelService.Value.GetFullListAsync(
                    z => z.ConfigModelType == ConfigModelType.Chat,
                    z => z.Id,
                    OrderingType.Descending);

                var candidates = chatModels
                    .Where(z => z.Id != excludeModelId)
                    .Take(6)
                    .Select(z => BuildModelDiagnosticInfo(new AIModelDto(z)))
                    .ToList();

                if (candidates.Count == 0)
                {
                    return "（无可用候选）";
                }

                return string.Join(" | ", candidates);
            }
            catch (Exception ex)
            {
                return $"（读取候选失败：{ex.GetType().Name} {ex.Message}）";
            }
        }

        private async Task<(string Output, UsageDetails Usage)> ExecuteChatCoreAsync(
            AIModelDto model,
            ChatOptions chatOptions,
            string runnerName,
            string prompt,
            List<ChatMessage> history,
            string scene,
            Action<AgentResponseUpdate> inStreamItemProceessing = null,
            SenparcAiSetting overrideSetting = null)
        {
            var chatModel = EnsureChatCompatibleModel(model, scene);
            var aiSettings = overrideSetting ?? _aiModelService.Value.BuildSenparcAiSetting(chatModel);
            SenparcTrace.SendCustomLog(
                "PromptRange.AI.Attempt",
                $"{scene} 开始请求。Runner={runnerName}, Stream={(inStreamItemProceessing != null)}, HistoryCount={history?.Count ?? 0}, Model={BuildModelDiagnosticInfo(chatModel)}");
            var iWantToRun = await new AgentAiHandler(aiSettings)
                .IWantTo(aiSettings)
                .ConfigChatModel(runnerName, BuildChatClientAgentOptions(CloneChatOptions(chatOptions)))
                .BuildKernelWithAgentSessionAsync();

            var agentSession = iWantToRun.Kernel.AgentSession
                ?? throw new NcfExceptionBase("AgentKernel 未创建 AgentSession，请检查 AI 模型配置是否为 Chat 类型");
            agentSession.SetInMemoryChatHistory(history ?? new List<ChatMessage>());

            var result = await RunChatAndExtractAsync(
                iWantToRun,
                prompt,
                agentSession,
                chatModel,
                scene,
                inStreamItemProceessing);
            var usage = PromptUsageHelper.ResolveUsage(result.Usage);
            SenparcTrace.SendCustomLog(
                "PromptRange.AI.Attempt",
                $"{scene} 请求成功。Runner={runnerName}, PromptTokens={usage.PromptTokens}, CompletionTokens={usage.CompletionTokens}, TotalTokens={usage.TotalTokens}");
            return result;
        }

        private static ChatOptions CloneChatOptions(ChatOptions chatOptions)
        {
            if (chatOptions == null)
            {
                return new ChatOptions();
            }

            return new ChatOptions
            {
                Instructions = chatOptions.Instructions,
                MaxOutputTokens = chatOptions.MaxOutputTokens,
                Temperature = chatOptions.Temperature,
                TopP = chatOptions.TopP,
                FrequencyPenalty = chatOptions.FrequencyPenalty,
                PresencePenalty = chatOptions.PresencePenalty,
                StopSequences = chatOptions.StopSequences?.ToList() ?? new List<string>(),
                Tools = chatOptions.Tools?.ToList()
            };
        }

        private static bool CanUseDefaultChatFallback(SenparcAiSetting defaultSetting)
        {
            if (defaultSetting == null || defaultSetting.AiPlatform == AiPlatform.UnSet)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(defaultSetting.ModelName?.Chat))
            {
                return false;
            }

            if (defaultSetting.AiPlatform != AiPlatform.Ollama && string.IsNullOrWhiteSpace(defaultSetting.ApiKey))
            {
                return false;
            }

            return defaultSetting.AiPlatform switch
            {
                AiPlatform.OpenAI => true,
                AiPlatform.Ollama => !string.IsNullOrWhiteSpace(defaultSetting.OllamaEndpoint),
                _ => !string.IsNullOrWhiteSpace(defaultSetting.Endpoint)
            };
        }

        private static AIModelDto BuildModelDtoFromDefaultSetting(SenparcAiSetting defaultSetting)
        {
            if (defaultSetting == null)
            {
                return null;
            }

            var apiVersion = defaultSetting.AiPlatform switch
            {
                AiPlatform.AzureOpenAI => defaultSetting.AzureOpenAIApiVersion,
                AiPlatform.NeuCharAI => defaultSetting.NeuCharAIApiVersion,
                _ => null
            };

            return new AIModelDto
            {
                Id = 0,
                Alias = "SystemDefaultChat",
                AiPlatform = defaultSetting.AiPlatform,
                ConfigModelType = ConfigModelType.Chat,
                ModelId = defaultSetting.ModelName?.Chat,
                DeploymentName = defaultSetting.DeploymentName ?? defaultSetting.ModelName?.Chat,
                Endpoint = defaultSetting.Endpoint,
                ApiKey = defaultSetting.ApiKey,
                ApiVersion = apiVersion
            };
        }

        private static bool IsSameChatConfig(AIModelDto left, AIModelDto right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.AiPlatform == right.AiPlatform
                   && string.Equals(
                       NormalizeEndpointForDiagnostics(left.AiPlatform, left.Endpoint),
                       NormalizeEndpointForDiagnostics(right.AiPlatform, right.Endpoint),
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(left.ModelId, right.ModelId, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(left.DeploymentName, right.DeploymentName, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(left.ApiKey, right.ApiKey, StringComparison.Ordinal);
        }

        private static AIModelDto EnsureChatCompatibleModel(AIModelDto model, string scene)
        {
            if (model == null)
            {
                return null;
            }

            if (model.ConfigModelType == ConfigModelType.Chat)
            {
                return model;
            }

            var deploymentName = model.DeploymentName;
            if ((model.AiPlatform == AiPlatform.AzureOpenAI || model.AiPlatform == AiPlatform.NeuCharAI)
                && string.IsNullOrWhiteSpace(deploymentName))
            {
                deploymentName = model.ModelId;
            }

            var chatModel = new AIModelDto
            {
                Id = model.Id,
                Alias = $"{model.Alias ?? "Model"}_ChatCompat",
                DeploymentName = deploymentName,
                ModelId = model.ModelId,
                Endpoint = model.Endpoint,
                AiPlatform = model.AiPlatform,
                ConfigModelType = ConfigModelType.Chat,
                OrganizationId = model.OrganizationId,
                ApiKey = model.ApiKey,
                ApiVersion = model.ApiVersion,
                Note = model.Note,
                MaxToken = model.MaxToken,
                IsShared = model.IsShared,
                Show = model.Show
            };

            SenparcTrace.SendCustomLog(
                "PromptRange.AI.ModelTypeCompat",
                $"{scene} 检测到非 Chat 模型配置（{model.ConfigModelType}），已临时按 Chat 方式兼容调用。Origin={BuildModelDiagnosticInfo(model)}; Compat={BuildModelDiagnosticInfo(chatModel)}");

            return chatModel;
        }

        private static bool TryBuildAlternateDeploymentModel(AIModelDto model, out AIModelDto fallbackModel)
        {
            fallbackModel = null;
            if (model == null)
            {
                return false;
            }

            if (model.AiPlatform != AiPlatform.AzureOpenAI && model.AiPlatform != AiPlatform.NeuCharAI)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(model.ModelId))
            {
                return false;
            }

            if (string.Equals(model.DeploymentName, model.ModelId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            fallbackModel = new AIModelDto
            {
                Id = model.Id,
                Alias = $"{model.Alias ?? "Model"}_DeploymentAsModelId",
                DeploymentName = model.ModelId,
                ModelId = model.ModelId,
                Endpoint = model.Endpoint,
                AiPlatform = model.AiPlatform,
                ConfigModelType = model.ConfigModelType,
                OrganizationId = model.OrganizationId,
                ApiKey = model.ApiKey,
                ApiVersion = model.ApiVersion,
                Note = model.Note,
                MaxToken = model.MaxToken,
                IsShared = model.IsShared,
                Show = model.Show
            };
            return true;
        }

        private static async Task<(string Output, UsageDetails Usage)> RunChatAndExtractAsync(
            IWantToRun iWantToRun,
            string prompt,
            AgentSession agentSession,
            AIModelDto model,
            string scene,
            Action<AgentResponseUpdate> inStreamItemProceessing = null)
        {
            // NeuCharAI 网关通常不支持稳定的流式返回（参考 AgentKernel 测试约定），
            // 在 PromptRange 中统一走非流式路径，避免 Stream / 非 Stream 记录链路不一致。
            if (inStreamItemProceessing != null && model?.AiPlatform == AiPlatform.NeuCharAI)
            {
                SenparcTrace.SendCustomLog("PromptRange.AI.StreamBypass",
                    $"{scene} 检测到 {AiPlatform.NeuCharAI}，跳过流式调用并切换为非流式执行。");
                return await RunChatDirectAsync(iWantToRun, prompt, agentSession, model, scene);
            }

            if (inStreamItemProceessing == null)
            {
                return await RunChatDirectAsync(iWantToRun, prompt, agentSession, model, scene);
            }

            SenparcKernelAiResult<string> aiResult;
            try
            {
                aiResult = await iWantToRun.RunChatAsync(prompt, agentSession, inStreamItemProceessing);
            }
            catch (NcfExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 兼容某些模型/网关在 Session 场景下的拒绝，先尝试去掉 Session 再请求一次
                if (agentSession != null)
                {
                    try
                    {
                        SenparcTrace.SendCustomLog("PromptRange.AI.Retry", $"{scene} 流式调用失败，尝试移除 AgentSession 后重试。");
                        aiResult = await iWantToRun.RunChatAsync(prompt, null, inStreamItemProceessing);
                    }
                    catch (Exception retryEx)
                    {
                        // 若流式失败（例如网关策略不允许 SSE），降级到非流式以尽可能返回结果
                        if (ContainsForbiddenStatus(retryEx))
                        {
                            SenparcTrace.SendCustomLog("PromptRange.AI.Fallback", $"{scene} 流式调用重试仍返回 403，降级为非流式请求。");
                            return await RunChatDirectAsync(iWantToRun, prompt, null, model, scene);
                        }

                        throw BuildAiCallException(retryEx, model, scene);
                    }
                }
                else if (ContainsForbiddenStatus(ex))
                {
                    // 无 Session 的流式请求被 403 时，降级到非流式
                    SenparcTrace.SendCustomLog("PromptRange.AI.Fallback", $"{scene} 流式调用返回 403，降级为非流式请求。");
                    return await RunChatDirectAsync(iWantToRun, prompt, null, model, scene);
                }
                else
                {
                    throw BuildAiCallException(ex, model, scene);
                }
            }

            EnsureRunChatSucceeded(aiResult, model, scene);
            return (aiResult.OutputString ?? string.Empty, aiResult.Result?.Usage);
        }

        private static async Task<(string Output, UsageDetails Usage)> RunChatDirectAsync(
            IWantToRun iWantToRun,
            string prompt,
            AgentSession agentSession,
            AIModelDto model,
            string scene)
        {
            try
            {
                var aiResult = await iWantToRun.RunChatAsync(prompt, agentSession);
                EnsureRunChatSucceeded(aiResult, model, scene);
                return (aiResult.OutputString ?? string.Empty, aiResult.Result?.Usage);
            }
            catch (NcfExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (agentSession != null)
                {
                    try
                    {
                        SenparcTrace.SendCustomLog("PromptRange.AI.Retry", $"{scene} 非流式调用失败，尝试移除 AgentSession 后重试。");
                        var retryResult = await iWantToRun.RunChatAsync(prompt, null);
                        EnsureRunChatSucceeded(retryResult, model, scene);
                        return (retryResult.OutputString ?? string.Empty, retryResult.Result?.Usage);
                    }
                    catch (Exception retryEx)
                    {
                        throw BuildAiCallException(retryEx, model, scene);
                    }
                }

                throw BuildAiCallException(ex, model, scene);
            }
        }

        private static NcfExceptionBase BuildAiCallException(Exception ex, AIModelDto model, string scene)
        {
            var detail = BuildModelDiagnosticInfo(model);
            var chain = FlattenExceptionMessages(ex);
            var isForbidden = chain.Contains("Status: 403 (Forbidden)", StringComparison.OrdinalIgnoreCase);
            var isSslTransportError = ContainsSslTransportError(ex);

            var messageBuilder = new StringBuilder();
            messageBuilder.Append($"AI 调用失败（{scene}）。{detail}");

            if (isForbidden)
            {
                messageBuilder.Append("；检测到上游返回 403（Forbidden），通常由模型端点权限、API Key、Deployment 名称或账号配额/策略导致。");

                // AdminChat 默认（aiModelId=0）会使用系统级 SenparcAiSetting，这里一并输出，
                // 便于快速对照“PromptRange 指定模型”和“系统默认模型”的差异。
                var defaultSettingInfo = BuildDefaultChatSettingDiagnosticInfo();
                if (!string.IsNullOrWhiteSpace(defaultSettingInfo))
                {
                    messageBuilder.AppendLine();
                    messageBuilder.Append("系统默认聊天配置：");
                    messageBuilder.Append(defaultSettingInfo);
                }
            }
            else if (isSslTransportError)
            {
                messageBuilder.Append("；检测到 TLS/SSL 连接在传输阶段被重置（Connection reset by peer）。这通常是网络链路、代理、中间设备或目标区域可达性问题，而不是 Prompt 或模型参数本身错误。");
            }

            if (!string.IsNullOrWhiteSpace(chain))
            {
                messageBuilder.AppendLine();
                messageBuilder.Append("原始错误链：");
                messageBuilder.Append(chain);
            }

            return new NcfExceptionBase(messageBuilder.ToString(), ex);
        }

        private static string BuildDefaultChatSettingDiagnosticInfo()
        {
            try
            {
                var defaultSetting = Senparc.AI.Config.SenparcAiSetting as SenparcAiSetting;
                if (defaultSetting == null)
                {
                    return "未读取到 SenparcAiSetting（null）";
                }

                var platform = defaultSetting.AiPlatform;
                var endpoint = NormalizeEndpointForDiagnostics(platform, defaultSetting.Endpoint);
                var apiKey = defaultSetting.ApiKey;
                var apiKeyStatus = string.IsNullOrWhiteSpace(apiKey)
                    ? "empty"
                    : $"set(len:{apiKey.Length})";
                var chatModel = defaultSetting.ModelName?.Chat ?? "(null)";
                var deployment = defaultSetting.DeploymentName ?? "(null)";

                return $"Platform={platform}, ChatModel={chatModel}, Deployment={deployment}, Endpoint={endpoint ?? "(null)"}, ApiKey={apiKeyStatus}";
            }
            catch (Exception ex)
            {
                return $"读取 SenparcAiSetting 失败：{ex.GetType().Name} {ex.Message}";
            }
        }

        private static void EnsureRunChatSucceeded(SenparcKernelAiResult<string> aiResult, AIModelDto model, string scene)
        {
            if (aiResult?.Result != null)
            {
                return;
            }

            var fallback = aiResult?.OutputString;
            if (string.IsNullOrWhiteSpace(fallback))
            {
                fallback = "RunChatAsync 未返回有效 AgentResponse（Result 为 null）。";
            }

            throw BuildAiCallException(new Exception(fallback), model, scene);
        }

        private static string BuildModelDiagnosticInfo(AIModelDto model)
        {
            if (model == null)
            {
                return "模型配置为空（AIModelDto == null）";
            }

            var endpoint = NormalizeEndpointForDiagnostics(model.AiPlatform, model.Endpoint);
            var apiKeyStatus = string.IsNullOrWhiteSpace(model.ApiKey)
                ? "empty"
                : $"set(len:{model.ApiKey.Length})";

            return $"AIModelDbId={model.Id}, ConfigType={model.ConfigModelType}, ModelId={model.ModelId ?? "(null)"}, Alias={model.Alias ?? "(null)"}, Platform={model.AiPlatform}, Deployment={model.DeploymentName ?? "(null)"}, Endpoint={endpoint ?? "(null)"}, ApiVersion={model.ApiVersion ?? "(null)"}, ApiKey={apiKeyStatus}";
        }

        private static string CreateTraceToken()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private static string NormalizeEndpointForDiagnostics(AiPlatform platform, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return endpoint;
            }

            var normalized = endpoint.Trim();
            if (platform == AiPlatform.NeuCharAI && !normalized.EndsWith("/", StringComparison.Ordinal))
            {
                normalized += "/";
            }

            return normalized;
        }

        private static string FlattenExceptionMessages(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var current = ex;
            var depth = 0;
            while (current != null && depth < 8)
            {
                if (depth > 0)
                {
                    sb.Append(" -> ");
                }

                sb.Append('[');
                sb.Append(current.GetType().Name);
                sb.Append("] ");
                sb.Append(current.Message?.Trim());

                current = current.InnerException;
                depth++;
            }

            return sb.ToString();
        }

        private static bool ContainsForbiddenStatus(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            var chain = FlattenExceptionMessages(ex);
            if (chain.Contains("Status: 403 (Forbidden)", StringComparison.OrdinalIgnoreCase)
                || chain.Contains("StatusCode: 403", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var has403 = chain.Contains("403", StringComparison.OrdinalIgnoreCase);
            var hasForbidden = chain.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                               || chain.Contains("禁止", StringComparison.OrdinalIgnoreCase)
                               || chain.Contains("拒绝", StringComparison.OrdinalIgnoreCase);

            return has403 && hasForbidden;
        }

        private static bool IsInMemoryVectorDbType(VectorDBType vectorDbType)
        {
            if (vectorDbType == VectorDBType.Memory)
            {
                return true;
            }

            // 兼容较新枚举成员（当前项目 UI 有 VolatileInMemory 选项）
            var typeName = vectorDbType.ToString();
            return string.Equals(typeName, "VolatileInMemory", StringComparison.OrdinalIgnoreCase);
        }

        private async Task EnsureTextEmbeddingSourceForInMemorySearchAsync(
            AIModelDto model,
            AIVectorDto vectorDto,
            PromptTextEmbeddingResultPayload embeddingPayload,
            int promptResultId,
            string scene)
        {
            if (embeddingPayload == null || string.IsNullOrWhiteSpace(embeddingPayload.SourceText))
            {
                return;
            }

            var collectionName = (embeddingPayload.CollectionName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                return;
            }

            var sourceId = ResolveEmbeddingSourceId(embeddingPayload.UpsertSourceId, promptResultId);
            var sourceName = string.IsNullOrWhiteSpace(embeddingPayload.UpsertSourceName)
                ? $"PromptResult-{promptResultId}"
                : embeddingPayload.UpsertSourceName;
            var sourceLink = string.IsNullOrWhiteSpace(embeddingPayload.UpsertSourceName)
                ? $"PromptRange://PromptResult/{promptResultId}"
                : $"PromptRange://{embeddingPayload.UpsertSourceName}";

            var aiSettings = _aiModelService.Value.BuildSenparcAiSetting(model, vectorDto);
            var runnerName = $"PromptRange_EmbeddingEnsure_{promptResultId}_{CreateTraceToken()}";

            SenparcTrace.SendCustomLog(
                "PromptRange.AI.Attempt",
                $"{scene} InMemory 向量库查询前补写文档。Runner={runnerName}, Collection={collectionName}, SourceId={sourceId}");

            var iWantToRun = new AgentAiHandler(aiSettings)
                .IWantTo(aiSettings)
                .ConfigTextEmbeddingModel(runnerName, collectionName)
                .BuildKernel();
            ApplyEmbeddingRequestCompatibility(iWantToRun, model, scene);

            var store = iWantToRun.CreateTextSearchStore();
            await store.UpsertDocumentsAsync(new List<TextSearchDocument>
            {
                new TextSearchDocument
                {
                    SourceId = sourceId,
                    SourceName = sourceName,
                    SourceLink = sourceLink,
                    Text = embeddingPayload.SourceText
                }
            });
        }

        private static ulong ResolveEmbeddingSourceId(string rawSourceId, int promptResultId)
        {
            if (ulong.TryParse(rawSourceId, out var parsed) && parsed > 0)
            {
                return parsed;
            }

            return (ulong)Math.Max(promptResultId, 1);
        }

        private static bool ContainsSslTransportError(Exception ex)
        {
            if (ex == null)
            {
                return false;
            }

            var chain = FlattenExceptionMessages(ex);
            if (string.IsNullOrWhiteSpace(chain))
            {
                return false;
            }

            return chain.Contains("ssl connection could not be established", StringComparison.OrdinalIgnoreCase)
                   || chain.Contains("authentication failed", StringComparison.OrdinalIgnoreCase)
                   || chain.Contains("connection reset by peer", StringComparison.OrdinalIgnoreCase)
                   || chain.Contains("transport connection", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<List<PromptTextEmbeddingSearchHitPayload>> SearchTextEmbeddingStoreAsync(
            AIModelDto model,
            AIVectorDto vectorDto,
            string collectionName,
            string query,
            int topK,
            string scene)
        {
            var aiSettings = _aiModelService.Value.BuildSenparcAiSetting(model, vectorDto);
            var runnerName = $"PromptRange_EmbeddingSearch_{model.Id}_{CreateTraceToken()}";

            SenparcTrace.SendCustomLog(
                "PromptRange.AI.Attempt",
                $"{scene} 开始请求。Runner={runnerName}, Model={BuildModelDiagnosticInfo(model)}, VectorDb={vectorDto?.Alias}, Collection={collectionName}, TopK={topK}");

            var iWantToRun = new AgentAiHandler(aiSettings)
                .IWantTo(aiSettings)
                .ConfigTextEmbeddingModel(runnerName, collectionName)
                .BuildKernel();
            ApplyEmbeddingRequestCompatibility(iWantToRun, model, scene);

            var store = iWantToRun.CreateTextSearchStore();
            var searchResults = await store.SearchAsync(query, topK);
            return MapTextEmbeddingSearchHits(searchResults);
        }

        private static void ApplyEmbeddingRequestCompatibility(IWantToRun iWantToRun, AIModelDto model, string scene)
        {
            if (iWantToRun?.Kernel?.EmbeddingClient is not EmbeddingClient embeddingClient)
            {
                return;
            }

            if (SupportsEmbeddingDimensionsParameter(model))
            {
                return;
            }

            // 某些模型（例如 text-embedding-ada-002）不支持请求中显式传入 dimensions。
            // 仍保留 Kernel.EmbeddingDimensions 用于向量库定义，但请求时改为不传该参数。
            iWantToRun.Kernel.EmbeddingGenerator = embeddingClient.AsIEmbeddingGenerator(null);
            SenparcTrace.SendCustomLog(
                "PromptRange.AI.Embedding",
                $"{scene} 检测到模型不支持 dimensions 请求参数，已切换为无 dimensions 请求。Model={BuildModelDiagnosticInfo(model)}");
        }

        private static bool SupportsEmbeddingDimensionsParameter(AIModelDto model)
        {
            if (model == null)
            {
                return false;
            }

            var hints = new[]
            {
                model.ModelId,
                model.DeploymentName,
                model.Alias
            };

            foreach (var hint in hints)
            {
                if (string.IsNullOrWhiteSpace(hint))
                {
                    continue;
                }

                var normalized = hint.Trim().ToLowerInvariant();
                if (normalized.Contains("text-embedding-3-small")
                    || normalized.Contains("text-embedding-3-large"))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<PromptTextEmbeddingSearchHitPayload> MapTextEmbeddingSearchHits(IEnumerable<TextSearchDocument> hits)
        {
            if (hits == null)
            {
                return new List<PromptTextEmbeddingSearchHitPayload>();
            }

            return hits.Select(item => new PromptTextEmbeddingSearchHitPayload
            {
                SourceId = item?.SourceId.ToString() ?? string.Empty,
                SourceName = item?.SourceName ?? string.Empty,
                SourceLink = item?.SourceLink ?? string.Empty,
                Text = item?.Text ?? string.Empty,
                Score = item == null ? 0 : Convert.ToSingle(item.Score)
            }).ToList();
        }

        private static string BuildEmbeddingCollectionName(string rawCollectionName, PromptItemDto promptItem, AIModelDto model)
        {
            var source = rawCollectionName;
            if (string.IsNullOrWhiteSpace(source))
            {
                source = $"PromptRange_{promptItem?.RangeName}_{promptItem?.Tactic}_{BuildEmbeddingModelCollectionSuffix(model)}";
            }

            var normalized = Regex.Replace(source.Trim(), @"[^\w\-\.:]", "_");
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "PromptRange_Embedding";
            }

            return normalized.Length > 128 ? normalized.Substring(0, 128) : normalized;
        }

        private static string BuildEmbeddingModelCollectionSuffix(AIModelDto model)
        {
            var raw = model?.ModelId;
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = model?.DeploymentName;
            }
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = model?.Alias;
            }
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "default";
            }

            var normalized = Regex.Replace(raw.Trim(), @"[^\w\-\.:]", "_");
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "default";
            }

            return normalized.Length > 32 ? normalized.Substring(0, 32) : normalized;
        }

        private static ulong CreateEmbeddingSourceId()
        {
            var next = Interlocked.Increment(ref _embeddingSourceCounter);
            if (next <= 0)
            {
                next = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Interlocked.Exchange(ref _embeddingSourceCounter, next);
            }

            return (ulong)next;
        }

        private static int NormalizeEmbeddingSearchTopK(int? rawTopK)
        {
            var topK = rawTopK ?? DefaultEmbeddingSearchTopK;
            if (topK < 1)
            {
                topK = 1;
            }

            if (topK > MaxEmbeddingSearchTopK)
            {
                topK = MaxEmbeddingSearchTopK;
            }

            return topK;
        }

        private static string NormalizeTextToSpeechVoice(string rawVoice)
        {
            return string.IsNullOrWhiteSpace(rawVoice) ? "alloy" : rawVoice.Trim().ToLowerInvariant();
        }

        private static string NormalizeTextToSpeechFormat(string rawFormat)
        {
            return rawFormat?.Trim().ToLowerInvariant() switch
            {
                "mp3" => "mp3",
                "opus" => "opus",
                "aac" => "aac",
                "flac" => "flac",
                "wav" => "wav",
                "pcm" => "pcm",
                _ => "mp3"
            };
        }

        private static float NormalizeTextToSpeechSpeed(float? rawSpeed)
        {
            var speed = rawSpeed ?? 1.0f;
            if (float.IsNaN(speed) || float.IsInfinity(speed))
            {
                speed = 1.0f;
            }

            return Math.Clamp(speed, 0.5f, 2.0f);
        }

        private async Task<(string AudioPath, string AudioRelativePath, string AudioFileName, string AudioContentType, long AudioBytesLength)> ResolveSpeechToTextAudioAsync(
            PromptSpeechToTextExecutionOptions options)
        {
            if (options == null)
            {
                throw new NcfExceptionBase("STT 需要上传音频文件");
            }

            if (!string.IsNullOrWhiteSpace(options.AudioLocalRelativePath))
            {
                var normalizedRelativePath = NormalizePromptAssetRelativePath(options.AudioLocalRelativePath);
                if (string.IsNullOrWhiteSpace(normalizedRelativePath))
                {
                    throw new NcfExceptionBase("STT 音频路径无效");
                }

                if (!normalizedRelativePath.StartsWith("PromptRange/SpeechToText/Input", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NcfExceptionBase("STT 音频路径不合法");
                }

                var fullPath = BuildAppDataAbsolutePath(normalizedRelativePath);
                if (!File.Exists(fullPath))
                {
                    throw new NcfExceptionBase("STT 音频文件不存在，请重新上传");
                }

                var fileInfo = new FileInfo(fullPath);
                var existingAudioContentType = DetectAudioContentType(Path.GetExtension(fullPath));
                return (
                    fullPath,
                    normalizedRelativePath,
                    fileInfo.Name,
                    existingAudioContentType,
                    fileInfo.Length);
            }

            if (string.IsNullOrWhiteSpace(options.AudioBase64))
            {
                throw new NcfExceptionBase("STT 需要上传音频文件");
            }

            var audioBytes = DecodeAudioBase64(options.AudioBase64, out var audioContentTypeFromPayload);
            if (audioBytes == null || audioBytes.Length == 0)
            {
                throw new NcfExceptionBase("上传的 STT 音频内容为空");
            }

            var extension = ResolveAudioExtension(options.AudioFileName, audioContentTypeFromPayload);
            var localRelativePath = await SavePromptAudioToAppDataAsync(
                audioBytes,
                "SpeechToText/Input",
                extension,
                "stt_input");
            var audioPath = BuildAppDataAbsolutePath(localRelativePath);
            var audioFileName = string.IsNullOrWhiteSpace(options.AudioFileName)
                ? Path.GetFileName(audioPath)
                : options.AudioFileName.Trim();
            var contentType = DetectAudioContentType(extension);

            return (
                audioPath,
                localRelativePath,
                audioFileName,
                contentType,
                audioBytes.LongLength);
        }

        private static byte[] DecodeAudioBase64(string rawAudioBase64, out string contentType)
        {
            contentType = null;
            if (string.IsNullOrWhiteSpace(rawAudioBase64))
            {
                return null;
            }

            var payload = rawAudioBase64.Trim();
            if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = payload.IndexOf(',', StringComparison.Ordinal);
                if (commaIndex > 5)
                {
                    var meta = payload.Substring(5, commaIndex - 5);
                    var split = meta.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    contentType = split.Length > 0 ? split[0] : null;
                    payload = payload.Substring(commaIndex + 1);
                }
            }

            try
            {
                return Convert.FromBase64String(payload);
            }
            catch (FormatException ex)
            {
                throw new NcfExceptionBase("上传的 STT 音频 Base64 格式不正确", ex);
            }
        }

        private static string ResolveAudioExtension(string fileName, string contentType)
        {
            var extension = Path.GetExtension(fileName);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                return extension;
            }

            return contentType?.Trim().ToLowerInvariant() switch
            {
                "audio/mpeg" => ".mp3",
                "audio/mp3" => ".mp3",
                "audio/mp4" => ".mp4",
                "audio/x-m4a" => ".m4a",
                "audio/webm" => ".webm",
                "audio/wav" => ".wav",
                "audio/x-wav" => ".wav",
                "audio/flac" => ".flac",
                "audio/ogg" => ".ogg",
                "audio/oga" => ".oga",
                "audio/aac" => ".aac",
                _ => ".mp3"
            };
        }

        private static string NormalizeAudioExtensionByFormat(string format)
        {
            var normalizedFormat = NormalizeTextToSpeechFormat(format);
            return normalizedFormat switch
            {
                "mp3" => ".mp3",
                "opus" => ".opus",
                "aac" => ".aac",
                "flac" => ".flac",
                "wav" => ".wav",
                "pcm" => ".pcm",
                _ => ".mp3"
            };
        }

        private static string DetectAudioContentType(string extension)
        {
            return extension?.Trim().ToLowerInvariant() switch
            {
                ".mp3" => "audio/mpeg",
                ".opus" => "audio/opus",
                ".aac" => "audio/aac",
                ".flac" => "audio/flac",
                ".wav" => "audio/wav",
                ".pcm" => "audio/L16",
                ".m4a" => "audio/x-m4a",
                ".mp4" => "audio/mp4",
                ".webm" => "audio/webm",
                ".oga" => "audio/oga",
                ".ogg" => "audio/ogg",
                _ => "application/octet-stream"
            };
        }

        private static async Task<string> SavePromptAudioToAppDataAsync(
            byte[] audioBytes,
            string subDirectory,
            string extension,
            string filePrefix)
        {
            var normalizedExtension = NormalizeAudioExtension(extension);
            var now = SystemTime.Now;
            var relativeDirectory = Path.Combine(
                "PromptRange",
                subDirectory,
                now.ToString("yyyy"),
                now.ToString("MM"),
                now.ToString("dd"));
            var appDataRoot = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            var saveDirectory = Path.Combine(appDataRoot, relativeDirectory);
            Directory.CreateDirectory(saveDirectory);

            var fileName = $"{filePrefix}_{now:HHmmssfff}_{Guid.NewGuid():N}{normalizedExtension}";
            var fullPath = Path.Combine(saveDirectory, fileName);
            await File.WriteAllBytesAsync(fullPath, audioBytes);

            var relativePath = Path.Combine(relativeDirectory, fileName);
            return relativePath.Replace('\\', '/');
        }

        private static string BuildPromptAudioApiUrl(string localRelativePath)
        {
            var normalizedPath = (localRelativePath ?? string.Empty)
                .Replace('\\', '/')
                .TrimStart('/');
            var escapedPath = Uri.EscapeDataString(normalizedPath);
            return $"/api/Senparc.Xncf.PromptRange/PromptAudio/Get?path={escapedPath}";
        }

        private static string NormalizeAudioExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return ".mp3";
            }

            return extension.StartsWith(".", StringComparison.Ordinal)
                ? extension.Trim().ToLowerInvariant()
                : $".{extension.Trim().ToLowerInvariant()}";
        }

        private static string NormalizePromptAssetRelativePath(string path)
        {
            var normalized = (path ?? string.Empty).Trim().Replace('\\', '/');
            while (normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(1);
            }

            if (normalized.Contains("../", StringComparison.Ordinal) ||
                normalized.Contains("..\\", StringComparison.Ordinal))
            {
                return null;
            }

            return normalized;
        }

        private static string BuildAppDataAbsolutePath(string relativePath)
        {
            var appDataRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "App_Data"));
            var normalizedPath = NormalizePromptAssetRelativePath(relativePath)
                ?? throw new NcfExceptionBase("文件路径不合法");

            var fullPath = Path.GetFullPath(Path.Combine(appDataRoot, normalizedPath.Replace('/', Path.DirectorySeparatorChar)));
            var expectedPrefix = appDataRoot.EndsWith(Path.DirectorySeparatorChar)
                ? appDataRoot
                : appDataRoot + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new NcfExceptionBase("文件路径越界");
            }

            return fullPath;
        }

        private async Task<PromptTextToImageResultPayload> BuildTextToImagePayloadAsync(
            string prompt,
            string revisedPrompt,
            BinaryData imageBytes,
            Uri imageUri)
        {
            var payload = new PromptTextToImageResultPayload
            {
                Prompt = prompt,
                RevisedPrompt = revisedPrompt,
                RemoteImageUrl = imageUri?.ToString(),
                Width = DefaultTextToImageWidth,
                Height = DefaultTextToImageHeight,
                StorageType = TextToImageStorageTypeNone
            };

            var imageBinary = imageBytes?.ToArray();
            if (imageBinary != null && imageBinary.Length > 0)
            {
                var (extension, contentType) = DetectImageFormat(imageBinary, payload.RemoteImageUrl);
                var localRelativePath = await SaveImageToAppDataAsync(imageBinary, extension);
                payload.StorageType = TextToImageStorageTypeLocal;
                payload.LocalRelativePath = localRelativePath;
                payload.ContentType = contentType;
                payload.ImageUrl = BuildPromptImageApiUrl(localRelativePath);
                return payload;
            }

            if (!string.IsNullOrWhiteSpace(payload.RemoteImageUrl))
            {
                payload.StorageType = TextToImageStorageTypeRemote;
                payload.ContentType = DetectImageContentTypeFromUrl(payload.RemoteImageUrl) ?? "image/png";
                payload.ImageUrl = payload.RemoteImageUrl;
            }

            return payload;
        }

        private static async Task<string> SaveImageToAppDataAsync(byte[] imageBytes, string extension)
        {
            var normalizedExtension = NormalizeImageExtension(extension);
            var now = SystemTime.Now;
            var relativeDirectory = Path.Combine(
                "PromptRange",
                "TextToImage",
                now.ToString("yyyy"),
                now.ToString("MM"),
                now.ToString("dd"));
            var appDataRoot = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            var saveDirectory = Path.Combine(appDataRoot, relativeDirectory);
            Directory.CreateDirectory(saveDirectory);

            var fileName = $"img_{now:HHmmssfff}_{Guid.NewGuid():N}{normalizedExtension}";
            var fullPath = Path.Combine(saveDirectory, fileName);
            await File.WriteAllBytesAsync(fullPath, imageBytes);

            var relativePath = Path.Combine(relativeDirectory, fileName);
            return relativePath.Replace('\\', '/');
        }

        private static string BuildPromptImageApiUrl(string localRelativePath)
        {
            var normalizedPath = (localRelativePath ?? string.Empty)
                .Replace('\\', '/')
                .TrimStart('/');
            var escapedPath = Uri.EscapeDataString(normalizedPath);
            return $"/api/Senparc.Xncf.PromptRange/PromptImage/Get?path={escapedPath}";
        }

        private static string NormalizeImageExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return ".png";
            }

            return extension.StartsWith(".", StringComparison.Ordinal)
                ? extension.ToLowerInvariant()
                : $".{extension.ToLowerInvariant()}";
        }

        private static (string Extension, string ContentType) DetectImageFormat(byte[] imageBytes, string remoteImageUrl)
        {
            if (imageBytes != null && imageBytes.Length >= 8)
            {
                if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                {
                    return (".png", "image/png");
                }

                if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                {
                    return (".jpg", "image/jpeg");
                }

                if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x38)
                {
                    return (".gif", "image/gif");
                }

                if (imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x46
                    && imageBytes[8] == 0x57 && imageBytes[9] == 0x45 && imageBytes[10] == 0x42 && imageBytes[11] == 0x50)
                {
                    return (".webp", "image/webp");
                }
            }

            var contentTypeFromUrl = DetectImageContentTypeFromUrl(remoteImageUrl);
            if (!string.IsNullOrWhiteSpace(contentTypeFromUrl))
            {
                return contentTypeFromUrl switch
                {
                    "image/jpeg" => (".jpg", "image/jpeg"),
                    "image/gif" => (".gif", "image/gif"),
                    "image/webp" => (".webp", "image/webp"),
                    _ => (".png", "image/png")
                };
            }

            return (".png", "image/png");
        }

        private static string DetectImageContentTypeFromUrl(string remoteImageUrl)
        {
            if (string.IsNullOrWhiteSpace(remoteImageUrl))
            {
                return null;
            }

            if (!Uri.TryCreate(remoteImageUrl, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var extension = Path.GetExtension(uri.AbsolutePath)?.ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".png" => "image/png",
                _ => null
            };
        }

        private static int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            var effectiveLength = text.Trim().Length;
            return Math.Max(1, (int)Math.Ceiling(effectiveLength / 4d));
        }

        private string GetResultTextForScoring(PromptResult promptResult)
        {
            var fallbackResult = promptResult?.ResultString ?? string.Empty;
            if (promptResult == null)
            {
                return fallbackResult;
            }

            if (promptResult.TestType == TestType.Graph && TryParseTextToImagePayload(promptResult.ResultString, out var payload))
            {
                var details = new List<string>();
                if (!string.IsNullOrWhiteSpace(payload.RevisedPrompt))
                {
                    details.Add($"RevisedPrompt: {payload.RevisedPrompt}");
                }

                if (!string.IsNullOrWhiteSpace(payload.Prompt))
                {
                    details.Add($"Prompt: {payload.Prompt}");
                }

                if (!string.IsNullOrWhiteSpace(payload.ImageUrl))
                {
                    details.Add($"ImageUrl: {payload.ImageUrl}");
                }

                if (!string.IsNullOrWhiteSpace(payload.RemoteImageUrl))
                {
                    details.Add($"RemoteImageUrl: {payload.RemoteImageUrl}");
                }

                return details.Count > 0 ? string.Join(Environment.NewLine, details) : fallbackResult;
            }

            if (TryParseSpeechToTextPayload(promptResult.ResultString, out var sttPayload))
            {
                return sttPayload.Transcript ?? fallbackResult;
            }

            if (TryParseTextToSpeechPayload(promptResult.ResultString, out var ttsPayload))
            {
                return ttsPayload.Text ?? fallbackResult;
            }

            if (TryParseTextEmbeddingPayload(promptResult.ResultString, out var embeddingPayload))
            {
                var details = new List<string>
                {
                    $"Collection: {embeddingPayload.CollectionName}",
                    $"VectorDb: {embeddingPayload.VectorDbAlias}",
                    $"Text: {embeddingPayload.SourceText}"
                };
                if (embeddingPayload.SearchHits != null && embeddingPayload.SearchHits.Count > 0)
                {
                    details.AddRange(embeddingPayload.SearchHits.Select(z => $"Hit[{z.Score:0.###}]: {z.Text}"));
                }
                return string.Join(Environment.NewLine, details);
            }

            return fallbackResult;
        }

        private static bool TryParseTextToImagePayload(string rawResult, out PromptTextToImageResultPayload payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(rawResult))
            {
                return false;
            }

            try
            {
                payload = JsonSerializer.Deserialize<PromptTextToImageResultPayload>(
                    rawResult,
                    PromptResultJsonSerializerOptions);
                return payload != null
                       && string.Equals(payload.ResultType, TextToImageResultTypeTag, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                payload = null;
                return false;
            }
        }

        private static bool TryParseTextToSpeechPayload(string rawResult, out PromptTextToSpeechResultPayload payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(rawResult))
            {
                return false;
            }

            try
            {
                payload = JsonSerializer.Deserialize<PromptTextToSpeechResultPayload>(
                    rawResult,
                    PromptResultJsonSerializerOptions);
                return payload != null
                       && string.Equals(payload.ResultType, TextToSpeechResultTypeTag, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                payload = null;
                return false;
            }
        }

        private static bool TryParseSpeechToTextPayload(string rawResult, out PromptSpeechToTextResultPayload payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(rawResult))
            {
                return false;
            }

            try
            {
                payload = JsonSerializer.Deserialize<PromptSpeechToTextResultPayload>(
                    rawResult,
                    PromptResultJsonSerializerOptions);
                return payload != null
                       && string.Equals(payload.ResultType, SpeechToTextResultTypeTag, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                payload = null;
                return false;
            }
        }

        private static bool TryParseTextEmbeddingPayload(string rawResult, out PromptTextEmbeddingResultPayload payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(rawResult))
            {
                return false;
            }

            try
            {
                payload = JsonSerializer.Deserialize<PromptTextEmbeddingResultPayload>(
                    rawResult,
                    PromptResultJsonSerializerOptions);
                return payload != null
                       && string.Equals(payload.ResultType, TextEmbeddingResultTypeTag, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                payload = null;
                return false;
            }
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

        private sealed class PromptTextToImageResultPayload
        {
            [JsonPropertyName("__promptRangeResultType")]
            public string ResultType { get; set; } = TextToImageResultTypeTag;

            public string StorageType { get; set; } = TextToImageStorageTypeNone;

            public string Prompt { get; set; }

            public string RevisedPrompt { get; set; }

            public string ImageUrl { get; set; }

            public string RemoteImageUrl { get; set; }

            public string LocalRelativePath { get; set; }

            public string ContentType { get; set; }

            public int Width { get; set; } = DefaultTextToImageWidth;

            public int Height { get; set; } = DefaultTextToImageHeight;

            public string TokenStatSource { get; set; } = "Estimated";
        }

        private sealed class PromptTextToSpeechResultPayload
        {
            [JsonPropertyName("__promptRangeResultType")]
            public string ResultType { get; set; } = TextToSpeechResultTypeTag;

            public string StorageType { get; set; } = PromptAudioStorageTypeNone;

            public string Text { get; set; }

            public string Voice { get; set; }

            public string Format { get; set; }

            public float SpeedRatio { get; set; } = 1.0f;

            public string AudioUrl { get; set; }

            public string LocalRelativePath { get; set; }

            public string ContentType { get; set; }

            public long AudioBytesLength { get; set; }

            public string TokenStatSource { get; set; } = "Estimated";
        }

        private sealed class PromptSpeechToTextResultPayload
        {
            [JsonPropertyName("__promptRangeResultType")]
            public string ResultType { get; set; } = SpeechToTextResultTypeTag;

            public string Transcript { get; set; }

            public string Language { get; set; }

            public string Duration { get; set; }

            public string SourceAudioUrl { get; set; }

            public string SourceAudioLocalRelativePath { get; set; }

            public string SourceAudioFileName { get; set; }

            public string SourceAudioContentType { get; set; }

            public long SourceAudioBytesLength { get; set; }

            public string TokenStatSource { get; set; } = "Estimated";
        }

        private sealed class PromptTextEmbeddingResultPayload
        {
            [JsonPropertyName("__promptRangeResultType")]
            public string ResultType { get; set; } = TextEmbeddingResultTypeTag;

            public int VectorDbId { get; set; }

            public string VectorDbAlias { get; set; }

            public string VectorDbType { get; set; }

            public string CollectionName { get; set; }

            public string SourceText { get; set; }

            public int SourceTextLength { get; set; }

            public int EmbeddingDimension { get; set; }

            public string UpsertSourceId { get; set; }

            public string UpsertSourceName { get; set; }

            public string SearchQuery { get; set; }

            public int SearchTopK { get; set; } = DefaultEmbeddingSearchTopK;

            public List<PromptTextEmbeddingSearchHitPayload> SearchHits { get; set; } = new();

            public string TokenStatSource { get; set; } = "Estimated";
        }

        private sealed class PromptTextEmbeddingSearchHitPayload
        {
            public string SourceId { get; set; }

            public string SourceName { get; set; }

            public string SourceLink { get; set; }

            public string Text { get; set; }

            public float Score { get; set; }
        }
    }
}
