/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AIModelService.cs
    文件功能描述：AIModelService 服务逻辑
    
    
    创建标识：Senparc - 20231229
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260705
    修改描述：v0.13.4-preview3 修复 AI 模型类型展示顺序

    修改标识：Senparc - 20260707
    修改描述：v0.13.5-preview4 统一模型名称构建并补齐 Embedding 维度兼容处理

    修改标识：Senparc - 20260715
    修改描述：v0.13.5-preview4 升级 Senparc.AI 至 0.27.3 与 Senparc.AI.AgentKernel 至 0.1.10

    修改标识：Senparc - 20260718
    修改描述：同步 NeuChar 算力模型类型

    修改标识：Senparc - 20260722
    修改描述：同步 NeuChar 算力模型 API 版本

    修改标识：Senparc - 20260724
    修改描述：v0.14.0-preview5 同步 NeuChar 模型 API 版本并优化模型信息复制交互

    修改标识：Senparc - 20260815
    修改描述：v0.15.3-preview12 修复指定 AI 模型配置穿透并保留兼容 API Version

    修改标识：Senparc - 20260817
    修改描述：v0.15.4 Ollama 配置补齐 ModelName

    修改标识：Senparc - 20260822
    修改描述：v0.15.4 修复 Ollama 指定模型名称传递

    修改标识：Senparc - 20260915
    修改描述：v0.16.0 新增 AI Token 用量监控与模型选择能力

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.AI.Entities;
using Senparc.AI.Entities.Keys;
using Senparc.AI.Exceptions;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Models.Extensions;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    public class AIModelService : ServiceBase<AIModel>
    {
        public AIModelService(IRepositoryBase<AIModel> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public async Task<AIModelDto> AddAsync(AIModel_CreateOrEditRequest orEditRequest)
        {
            AIModel aiModel = new AIModel(orEditRequest);
            // var aIModel = _aIModelService.Mapper.Map<AIModel>(request);

            aiModel.SwitchShow(true);

            await this.SaveObjectAsync(aiModel);

            var aiModelDto = new AIModelDto(aiModel);

            return aiModelDto;
        }

        public async Task<AIModelDto> EditAsync(AIModel_CreateOrEditRequest request)
        {
            AIModel aiModel = await this.GetObjectAsync(z => z.Id == request.Id)
                              ?? throw new NcfExceptionBase("未查询到实体!");

            #region 如果字段为空就不更新

            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                request.ApiKey = aiModel.ApiKey;
            }
            if (string.IsNullOrWhiteSpace(request.OrganizationId))
            {
                request.OrganizationId = aiModel.OrganizationId;
            }

            #endregion

            aiModel.Update(request);

            await this.SaveObjectAsync(aiModel);

            var aiModelDto = new AIModelDto(aiModel);

            return aiModelDto;
        }

        /// <summary>
        /// 构造 SenparcAiSetting
        /// </summary>
        /// <param name="aiModel"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public SenparcAiSetting BuildSenparcAiSetting(AIModelDto aiModel, AIVectorDto aIVectorDto = null)
        {
            SenparcAiSetting aiSettings = new SenparcAiSetting
            {
                AiPlatform = aiModel.AiPlatform
            };
            var normalizedEndpoint = NormalizeEndpoint(aiModel.AiPlatform, aiModel.Endpoint);

            #region AI Model

            Func<ModelName> GetModelName = () =>
            {
                ModelName modelName = new();
                switch (aiModel.ConfigModelType)
                {
                    case Models.ConfigModelType.TextCompletion:
                        modelName.TextCompletion = aiModel.ModelId;
                        break;
                    case Models.ConfigModelType.Chat:
                        modelName.Chat = aiModel.ModelId;
                        break;
                    case Models.ConfigModelType.TextEmbedding:
                        modelName.Embedding = aiModel.ModelId;
                        break;
                    case Models.ConfigModelType.TextToImage:
                        modelName.TextToImage = aiModel.ModelId;
                        break;
                    case Models.ConfigModelType.ImageToText:
                        modelName.ImageToText = aiModel.ModelId;
                        break;
                    case Models.ConfigModelType.TextToSpeech:
                        modelName.TextToSpeech = aiModel.ModelId;
                        break;
                    case Models.ConfigModelType.SpeechToText:
                    case Models.ConfigModelType.SpeechRecognition:
                        modelName.SpeechToText = aiModel.ModelId;
                        break;
                    default:
                        throw new Exception($"尚未支持：{aiModel.ConfigModelType} 模型在 BuildSenparcAiSetting 中的处理");
                }

                ApplyEmbeddingModelMetadata(aiModel, modelName);
                return modelName;
            };

            var modelName = GetModelName();

            switch (aiSettings.AiPlatform)
            {
                case AiPlatform.NeuCharAI:
                    aiSettings.NeuCharAIKeys = new NeuCharAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        NeuCharAIApiVersion = GetApiVersionOrDefault(aiModel.ApiVersion),
                        NeuCharEndpoint = normalizedEndpoint,
                        ModelName = modelName,
                    };
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        AzureOpenAIApiVersion = GetApiVersionOrDefault(aiModel.ApiVersion),
                        AzureEndpoint = normalizedEndpoint,
                        ModelName = modelName,
                        DeploymentName = aiModel.DeploymentName
                    };
                    break;
                case AiPlatform.AzureOpenAI:
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        AzureOpenAIApiVersion = GetApiVersionOrDefault(aiModel.ApiVersion),
                        AzureEndpoint = normalizedEndpoint,
                        ModelName = modelName,
                        DeploymentName = aiModel.DeploymentName
                    };
                    break;
                case AiPlatform.HuggingFace:
                    aiSettings.HuggingFaceKeys = new HuggingFaceKeys()
                    {
                        Endpoint = normalizedEndpoint,
                        ModelName = modelName,
                    };
                    break;
                case AiPlatform.OpenAI:
                    aiSettings.OpenAIKeys = new OpenAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        OrganizationId = aiModel.OrganizationId,
                        ModelName = modelName
                    };
                    break;
                case AiPlatform.FastAPI:
                    aiSettings.FastAPIKeys = new FastAPIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        Endpoint = normalizedEndpoint,
                    };
                    break;
                case AiPlatform.Ollama:
                    aiSettings.OllamaKeys = new OllamaKeys()
                    {
                        Endpoint = normalizedEndpoint,
                        ModelName = modelName,
                    };
                    break;
                case AiPlatform.DeepSeek:
                    aiSettings.DeepSeekKeys = new DeepSeekKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        Endpoint = normalizedEndpoint,
                        ModelName = modelName,
                    };
                    break;
                default:
                    throw new NcfExceptionBase($"Senparc.Xncf.AIKernel 暂时不支持 {aiSettings.AiPlatform} 类型");
            }
            #endregion

            #region VectorDB
            if (aIVectorDto != null)
            {
                aiSettings.VectorDB = new AI.Interfaces.VectorDB()
                {
                    Type = aIVectorDto.VectorDBType,
                    ConnectionString = aIVectorDto.ConnectionString
                };
            }
            #endregion

            return aiSettings;
        }

        /// <summary>
        /// 运行模型
        /// </summary>
        /// <param name="senparcAiSetting"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public async Task<SenparcKernelAiResult<string>> RunModelsync(SenparcAiSetting senparcAiSetting, string prompt, string systemMessage, string promptTemplate, PromptConfigParameter promptConfigParameter = null, AgentSession agentSession = null, Action<AgentResponseUpdate> inStreamItemProcessing = null)
        {
            if (senparcAiSetting == null)
            {
                throw new SenparcAiException("SenparcAiSetting 不能为空");
            }

            promptConfigParameter ??= new PromptConfigParameter()
            {
                MaxTokens = 2000,
                Temperature = 0.7,
                TopP = 0.5,
            };

            // The requested model must own the AgentKernel pipeline. Resolving the scoped
            // AgentAiHandler from DI here uses the system-default SenparcAiSetting and can make a
            // failed database model appear healthy when the default model succeeds.
            var agentAiHandler = CreateModelAgentHandler(senparcAiSetting);
            var chatOptions = new ChatClientAgentOptions()
            {
                ChatOptions = new Microsoft.Extensions.AI.ChatOptions()
                {
                    Instructions = systemMessage,
                    MaxOutputTokens = promptConfigParameter.MaxTokens,
                    Temperature = (float?)promptConfigParameter.Temperature,
                    TopP = (float?)promptConfigParameter.TopP,
                    StopSequences = promptConfigParameter.StopSequences ?? new List<string>()
                },
                ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions())
            };

            var iWantToRun = await agentAiHandler.IWantTo(senparcAiSetting)
                .ConfigChatModel("SenparcNCF", chatOptions)
                .BuildKernelWithAgentSessionAsync();

            //var request = iWantToRun.CreateRequest(prompt);
            var aiResult = await iWantToRun.RunChatAsync(prompt, agentSession, inStreamItemProcessing);
            return aiResult;
        }

        private static AgentAiHandler CreateModelAgentHandler(SenparcAiSetting senparcAiSetting)
        {
            return new AgentAiHandler(senparcAiSetting);
        }

        /// <summary>
        /// 运行模型并记录 Token 监控（含异步进度）。
        /// <para>
        /// 与 <see cref="RunModelsync"/> 相比，本方法会以流式方式运行，并在运行过程中通过
        /// <see cref="AITokenMonitorService"/> 发布 <see cref="AITokenProgressEvent"/>（异步进度），
        /// 运行结束后捕获 <c>UsageDetails</c> 并通过 <see cref="AITokenUsageService"/> 持久化记录。
        /// </para>
        /// </summary>
        /// <param name="aiModel">要运行的模型</param>
        /// <param name="prompt">用户提示词</param>
        /// <param name="systemMessage">系统消息（可选）</param>
        /// <param name="promptConfigParameter">模型参数（可选）</param>
        /// <param name="monitor">Token 监控服务（异步进度 + 实时聚合）</param>
        /// <param name="progress">可选的进度回调（在监控服务之上再通知调用方）</param>
        /// <param name="source">调用来源标记</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>模型运行结果（含 Usage）</returns>
        public async Task<SenparcKernelAiResult<string>> RunModelWithMonitorAsync(
            AIModelDto aiModel,
            string prompt,
            string systemMessage,
            PromptConfigParameter promptConfigParameter,
            AITokenMonitorService monitor,
            IProgress<AITokenProgressEvent> progress = null,
            string source = "Monitor",
            Guid? runId = null,
            CancellationToken cancellationToken = default)
        {
            if (aiModel == null)
            {
                throw new SenparcAiException("AIModelDto 不能为空");
            }
            if (monitor == null)
            {
                throw new SenparcAiException("AITokenMonitorService 不能为空");
            }

            promptConfigParameter ??= new PromptConfigParameter()
            {
                MaxTokens = 2000,
                Temperature = 0.7,
                TopP = 0.5,
            };

            var actualRunId = runId ?? Guid.NewGuid();
            var stopwatch = Stopwatch.StartNew();
            var senparcAiSetting = BuildSenparcAiSetting(aiModel);

            var agentAiHandler = CreateModelAgentHandler(senparcAiSetting);
            var chatOptions = new ChatClientAgentOptions()
            {
                ChatOptions = new Microsoft.Extensions.AI.ChatOptions()
                {
                    Instructions = systemMessage,
                    MaxOutputTokens = promptConfigParameter.MaxTokens,
                    Temperature = (float?)promptConfigParameter.Temperature,
                    TopP = (float?)promptConfigParameter.TopP,
                    StopSequences = promptConfigParameter.StopSequences ?? new List<string>()
                },
                ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions())
            };

            var iWantToRun = await agentAiHandler.IWantTo(senparcAiSetting)
                .ConfigChatModel("SenparcNCF", chatOptions)
                .BuildKernelWithAgentSessionAsync();

            var outputText = new StringBuilder();
            var lastPublish = DateTime.MinValue;
            var lastUsage = new AITokenUsageSnapshot();
            const int MinPublishIntervalMs = 100;

            void Publish(AITokenProgressStatus status, AITokenUsageSnapshot usage, string error, bool force = false)
            {
                var now = DateTime.Now;
                if (!force && (now - lastPublish).TotalMilliseconds < MinPublishIntervalMs)
                {
                    return;
                }
                lastPublish = now;
                var evt = new AITokenProgressEvent
                {
                    RunId = actualRunId,
                    ModelAlias = aiModel.Alias,
                    ModelId = aiModel.ModelId,
                    Status = status,
                    InputTokens = usage.InputTokens,
                    OutputTokens = usage.OutputTokens,
                    TotalTokens = usage.TotalTokens,
                    OutputPreview = TruncatePreview(outputText.ToString()),
                    ElapsedMs = (int)stopwatch.ElapsedMilliseconds,
                    Error = error
                };
                monitor.PublishProgress(evt);
                try
                {
                    progress?.Report(evt);
                }
                catch
                {
                    // 忽略调用方回调异常，避免影响主流程
                }
            }

            Publish(AITokenProgressStatus.Running, AITokenUsageSnapshot.Empty, null, force: true);

            SenparcKernelAiResult<string> aiResult = null;
            bool success = false;
            string error = null;
            try
            {
                aiResult = await iWantToRun.RunChatAsync(prompt, null, (AgentResponseUpdate update) =>
                {
                    if (update?.Text != null)
                    {
                        outputText.Append(update.Text);
                    }
                    var usageContent = update?.Contents?.FirstOrDefault(z => z is UsageContent) as UsageContent;
                    if (usageContent?.Details != null)
                    {
                        lastUsage = BuildUsageSnapshot(usageContent.Details);
                    }
                    else
                    {
                        // 流式过程中没有精确 usage 时，用已生成文本长度估算输出 Token（约 4 字符 ≈ 1 token）
                        lastUsage.OutputTokens = (long)Math.Ceiling(outputText.Length / 4.0);
                        lastUsage.TotalTokens = lastUsage.InputTokens + lastUsage.OutputTokens;
                    }
                    Publish(AITokenProgressStatus.Running, lastUsage, null);
                });
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                error = ex.Message;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // 运行结束后，用结果中的精确 Usage 覆盖估算值
                var finalUsage = BuildUsageSnapshot(aiResult?.Result?.Usage);
                if (finalUsage.IsEmpty && !lastUsage.IsEmpty)
                {
                    finalUsage = lastUsage;
                }
                finalUsage.Normalize();

                var finalStatus = success ? AITokenProgressStatus.Completed : AITokenProgressStatus.Failed;
                var finalEvt = new AITokenProgressEvent
                {
                    RunId = actualRunId,
                    ModelAlias = aiModel.Alias,
                    ModelId = aiModel.ModelId,
                    Status = finalStatus,
                    InputTokens = finalUsage.InputTokens,
                    OutputTokens = finalUsage.OutputTokens,
                    TotalTokens = finalUsage.TotalTokens,
                    OutputPreview = TruncatePreview(outputText.Length > 0 ? outputText.ToString() : (aiResult?.OutputString ?? string.Empty)),
                    ElapsedMs = (int)stopwatch.ElapsedMilliseconds,
                    Error = error
                };
                monitor.PublishProgress(finalEvt);
                try
                {
                    progress?.Report(finalEvt);
                }
                catch
                {
                    // 忽略
                }

                // 实时聚合
                monitor.Record(aiModel.Alias, finalUsage, (int)stopwatch.ElapsedMilliseconds, success);

                // 持久化记录
                try
                {
                    var usageService = this._serviceProvider.GetService<AITokenUsageService>();
                    if (usageService != null)
                    {
                        await usageService.RecordAsync(
                            aiModel.Alias,
                            aiModel.ModelId,
                            aiModel.DeploymentName,
                            aiModel.AiPlatform,
                            aiModel.ConfigModelType,
                            finalUsage,
                            (int)stopwatch.ElapsedMilliseconds,
                            success,
                            source,
                            error);
                    }
                }
                catch
                {
                    // 持久化失败不应影响模型运行结果返回
                }
            }

            return aiResult;
        }

        /// <summary>
        /// 从 <see cref="UsageDetails"/> 构建 Token 使用快照
        /// </summary>
        public static AITokenUsageSnapshot BuildUsageSnapshot(UsageDetails usage)
        {
            var snapshot = new AITokenUsageSnapshot();
            if (usage == null)
            {
                return snapshot;
            }
            snapshot.InputTokens = usage.InputTokenCount ?? 0;
            snapshot.OutputTokens = usage.OutputTokenCount ?? 0;
            snapshot.TotalTokens = usage.TotalTokenCount ?? 0;
            snapshot.CachedInputTokens = usage.CachedInputTokenCount ?? 0;
            snapshot.ReasoningTokens = usage.ReasoningTokenCount ?? 0;
            snapshot.Normalize();
            return snapshot;
        }

        private static string TruncatePreview(string text, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            text = text.Replace("\r", " ").Replace("\n", " ");
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        public async Task<string> UpdateModelsFromNeuCharAsync(NeuCharGetModelJsonResult modelResult, int developerId, string apiKey)
        {
            if (modelResult?.Result?.Data == null)
            {
                return "模型数据不存在，请检查是否已部署，或是否具备权限！";
            }

            var models = await base.GetFullListAsync(z => z.AiPlatform == AiPlatform.NeuCharAI);
            var updateCount = 0;
            var addCount = 0;
            foreach (var neucharModel in modelResult.Result.Data)
            {
                var model = await base.GetObjectAsync(z => z.DeploymentName == neucharModel.Name);
                var dto = new AIModel_CreateOrEditRequest()
                {
                    AiPlatform = AiPlatform.NeuCharAI,
                    ApiKey = apiKey,
                    Alias = $"NeuChar-{neucharModel.Name}",
                    DeploymentName = neucharModel.Name,
                    ModelId = neucharModel.Name,
                    ApiVersion = neucharModel.ApiVersion,
                    Endpoint = $"https://www.neuchar.com/{developerId}/",
                    ConfigModelType = neucharModel.ModelType is ConfigModel.Unknown or ConfigModel.Other
                                        ? Models.ConfigModelType.Chat
                                        : (Models.ConfigModelType)neucharModel.ModelType,
                    Note = $"从 NeuChar AI 导入（DevId:{developerId}）",
                    Show = true
                };

                //兼容尚未返回 ModelType 的旧接口数据
                if ((neucharModel.ModelType is ConfigModel.Unknown or ConfigModel.Other) &&
                    neucharModel.Name.Contains("embedding", StringComparison.OrdinalIgnoreCase))
                {
                    dto.ConfigModelType = Models.ConfigModelType.TextEmbedding;
                }
                else if ((neucharModel.ModelType is ConfigModel.Unknown or ConfigModel.Other) &&
                         neucharModel.Name.Contains("text-davinci", StringComparison.OrdinalIgnoreCase))
                {
                    dto.ConfigModelType = Models.ConfigModelType.TextCompletion;
                }

                if (model == null)
                {
                    model = new AIModel(dto);
                    addCount++;
                }
                else
                {
                    if (!model.Note.IsNullOrEmpty())
                    {
                        dto.Note = model.Note;
                    }
                    dto.MaxToken = model.MaxToken;
                    dto.Alias = model.Alias;
                    model.Update(dto);

                    updateCount++;
                }

                await base.SaveObjectAsync(model);
            }
            return $"已成功添加 {addCount} 个模型，更新 {updateCount} 个模型信息。";
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
            var normalizedEndpoint = NormalizeEndpoint(llModel.AiPlatform, llModel.Endpoint);
            var modelName = BuildUnifiedModelName(llModel);

            switch (aiSettings.AiPlatform)
            {
                case AiPlatform.NeuCharAI:
                    aiSettings.NeuCharAIKeys = new NeuCharAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        NeuCharAIApiVersion = GetApiVersionOrDefault(llModel.ApiVersion),
                        NeuCharEndpoint = normalizedEndpoint,
                        ModelName = modelName
                    };
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        AzureOpenAIApiVersion = GetApiVersionOrDefault(llModel.ApiVersion),
                        AzureEndpoint = normalizedEndpoint,
                        DeploymentName = llModel.DeploymentName,
                        ModelName = modelName
                    };
                    break;
                case AiPlatform.AzureOpenAI:
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        AzureOpenAIApiVersion = GetApiVersionOrDefault(llModel.ApiVersion),
                        AzureEndpoint = normalizedEndpoint,
                        DeploymentName = llModel.DeploymentName,
                        ModelName = modelName
                    };
                    break;
                case AiPlatform.HuggingFace:
                    aiSettings.HuggingFaceKeys = new HuggingFaceKeys()
                    {
                        Endpoint = normalizedEndpoint,
                        ModelName = modelName
                    };
                    break;
                case AiPlatform.OpenAI:
                    aiSettings.OpenAIKeys = new OpenAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        OrganizationId = llModel.OrganizationId,
                        ModelName = modelName
                    };
                    break;
                case AiPlatform.FastAPI:
                    aiSettings.FastAPIKeys = new FastAPIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        Endpoint = normalizedEndpoint,
                        //OrganizationId = aiModel.OrganizationId
                        ModelName = modelName
                    };
                    break;
                case AiPlatform.Ollama:
                    aiSettings.OllamaKeys = new OllamaKeys()
                    {
                        Endpoint = normalizedEndpoint,
                        //OrganizationId = aiModel.OrganizationId
                        ModelName = modelName
                    };
                    break;
                case AiPlatform.DeepSeek:
                    aiSettings.DeepSeekKeys = new DeepSeekKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        Endpoint = normalizedEndpoint,
                        ModelName = modelName
                    };
                    break;
                default:
                    throw new NcfExceptionBase($"PromptRange 暂时不支持 {aiSettings.AiPlatform} 类型");
            }


            return aiSettings;
        }

        private static ModelName BuildUnifiedModelName(AIModelDto aiModel)
        {
            var modelName = new ModelName()
            {
                Chat = aiModel.ModelId,
                TextCompletion = aiModel.ModelId,
                Embedding = aiModel.ModelId,
                ImageToText = aiModel.ModelId,
                TextToImage = aiModel.ModelId,
                TextToSpeech = aiModel.ModelId,
                SpeechToText = aiModel.ModelId
            };

            ApplyEmbeddingModelMetadata(aiModel, modelName);
            return modelName;
        }

        private static void ApplyEmbeddingModelMetadata(AIModelDto aiModel, ModelName modelName)
        {
            if (aiModel == null || modelName == null || string.IsNullOrWhiteSpace(modelName.Embedding))
            {
                return;
            }

            var embeddingDimensions = ResolveEmbeddingDimensions(aiModel);
            if (embeddingDimensions > 0)
            {
                modelName.EmbeddingDimensions = embeddingDimensions;
            }
        }

        private static int ResolveEmbeddingDimensions(AIModelDto aiModel)
        {
            if (aiModel == null)
            {
                return 0;
            }

            if (TryParseEmbeddingDimensionsFromNote(aiModel.Note, out var noteDimensions))
            {
                return noteDimensions;
            }

            var hints = new[]
            {
                aiModel.ModelId,
                aiModel.DeploymentName,
                aiModel.Alias
            };

            foreach (var hint in hints)
            {
                if (string.IsNullOrWhiteSpace(hint))
                {
                    continue;
                }

                var normalized = hint.Trim().ToLowerInvariant();

                if (normalized.Contains("text-embedding-3-large"))
                {
                    return 3072;
                }

                if (normalized.Contains("text-embedding-3-small") || normalized.Contains("text-embedding-ada-002"))
                {
                    return 1536;
                }

                if (normalized.Contains("nomic-embed-text"))
                {
                    return 768;
                }

                if (normalized.Contains("mxbai-embed-large"))
                {
                    return 1024;
                }
            }

            // TextEmbedding 模型兜底，避免 AgentKernel 在创建 EmbeddingGenerator 时抛异常。
            return aiModel.ConfigModelType == ConfigModelType.TextEmbedding ? 1536 : 0;
        }

        private static bool TryParseEmbeddingDimensionsFromNote(string note, out int dimensions)
        {
            dimensions = 0;
            if (string.IsNullOrWhiteSpace(note))
            {
                return false;
            }

            var match = Regex.Match(note, @"(?i)\bembedding\s*dimensions?\s*[:=]\s*(\d{2,5})\b");
            if (!match.Success)
            {
                match = Regex.Match(note, @"(?i)\bdimensions?\s*[:=]\s*(\d{2,5})\b");
            }

            return match.Success
                   && int.TryParse(match.Groups[1].Value, out dimensions)
                   && dimensions > 0;
        }

        private static string NormalizeEndpoint(AiPlatform platform, string endpoint)
        {
            if (endpoint.IsNullOrWhiteSpace())
            {
                return endpoint;
            }

            var normalized = endpoint.Trim();

            // NeuChar endpoint usually contains a developer-id path segment.
            // Keep the segment by forcing a trailing slash (e.g. .../2/).
            if (platform == AiPlatform.NeuCharAI && !normalized.EndsWith("/", StringComparison.Ordinal))
            {
                normalized += "/";
            }

            return normalized;
        }

        private static string GetApiVersionOrDefault(string apiVersion)
        {
            // AzureOpenAIKeys / NeuCharAIKeys both define 2022-12-01 as the legacy-compatible
            // default. Do not replace it with null when an imported or manually created AIModel
            // does not specify ApiVersion; downstream AgentKernel execution needs the value.
            return string.IsNullOrWhiteSpace(apiVersion) ? "2022-12-01" : apiVersion.Trim();
        }

        /// <summary>
        /// 获取可用的 Chat 模型，如果当前 <see cref="AIModelDto"/> 对象可用，则保留
        /// </summary>
        /// <param name="currentModelDto"></param>
        /// <returns></returns>
        /// <exception cref="NcfExceptionBase"></exception>
        public async Task<(SenparcAiSetting AiSetting,AIModelDto FinalAiModelDto, bool ModelChanged)> GetValiableChatModel(AIModelDto currentModelDto)
        {
            // 获取模型
            AIModelDto aiModelDto;
            var modelChanged = false;
            //如果当前模型不是 Chat 类型，则需要找一个 Chat 模型
            if (currentModelDto.ConfigModelType != ConfigModelType.Chat)
            {
                var chatModel = await base.GetObjectAsync(z => z.ConfigModelType == ConfigModelType.Chat);
                if (chatModel == null)
                {
                    throw new NcfExceptionBase("必须至少设置一个 Chat 类型的模型才能自动打分（在 AIKernel 模块中）");
                }
                aiModelDto = base.Mapping<AIModelDto>(chatModel);
                modelChanged = true;
            }
            else
            {
                aiModelDto = currentModelDto;
            }

            // build aiSettings by model
            var aiSettings = this.BuildSenparcAiSetting(aiModelDto);
            return (aiSettings, aiModelDto, modelChanged);
        }
    }
}
