/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AIModelService.cs
    文件功能描述：AIModelService 服务逻辑
    
    
    创建标识：Senparc - 20231229
    
    修改标识：Senparc - 20260702
    修改描述：v0.11.0-preview2 同步 master/main 基线范围内改动并完成递归依赖版本处理

    修改标识：Senparc - 20260705
    修改描述：v0.13.4-preview3 修复 AI 模型类型展示顺序----------------------------------------------------------------*/

using Microsoft.Agents.AI;
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
                return modelName;
            };

            var modelName = GetModelName();

            switch (aiSettings.AiPlatform)
            {
                case AiPlatform.NeuCharAI:
                    aiSettings.NeuCharAIKeys = new NeuCharAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        NeuCharAIApiVersion = aiModel.ApiVersion, // SK中实际上没有用ApiVersion
                        NeuCharEndpoint = normalizedEndpoint,
                        ModelName = modelName,
                    };
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        AzureOpenAIApiVersion = aiModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = normalizedEndpoint,
                        ModelName = modelName,
                        DeploymentName = aiModel.DeploymentName
                    };
                    break;
                case AiPlatform.AzureOpenAI:
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        AzureOpenAIApiVersion = aiModel.ApiVersion, // SK中实际上没有用ApiVersion
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
        public async Task<SenparcKernelAiResult<string>> RunModelsync(SenparcAiSetting senparcAiSetting, string prompt, string systemMessage, string promptTemplate, PromptConfigParameter promptConfigParameter = null, AgentSession agentSession = null)
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

            var agentAiHandler = base._serviceProvider.GetService<AgentAiHandler>();
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

            var iWantToRun = await agentAiHandler.IWantTo()
                .ConfigChatModel("SenparcNCF", chatOptions)
                .BuildKernelWithAgentSessionAsync();

            //var request = iWantToRun.CreateRequest(prompt);
            var aiResult = await iWantToRun.RunChatAsync(prompt, agentSession);
            return aiResult;
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
                    ApiVersion = model?.AiPlatform == AiPlatform.AzureOpenAI || model?.AiPlatform == AiPlatform.OpenAI
                                    ? "2024-05-13"
                                    : "",
                    Endpoint = $"https://www.neuchar.com/{developerId}/",
                    ConfigModelType = Models.ConfigModelType.Chat,
                    Note = $"从 NeuChar AI 导入（DevId:{developerId}）",
                    Show = true
                };

                //TODO: 远程不提供，临时本地判断
                if (neucharModel.Name.Contains("embedding"))
                {
                    dto.ConfigModelType = Models.ConfigModelType.TextEmbedding;
                }
                else if (neucharModel.Name.Contains("text-davinci"))
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

            switch (aiSettings.AiPlatform)
            {
                case AiPlatform.NeuCharAI:
                    aiSettings.NeuCharAIKeys = new NeuCharAIKeys()
                    {
                        ApiKey = llModel.ApiKey,
                        NeuCharAIApiVersion = llModel.ApiVersion, // SK中实际上没有用ApiVersion
                        NeuCharEndpoint = normalizedEndpoint,
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
                        AzureEndpoint = normalizedEndpoint,
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
                        AzureEndpoint = normalizedEndpoint,
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
                        Endpoint = normalizedEndpoint,
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
                        Endpoint = normalizedEndpoint,
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
                        Endpoint = normalizedEndpoint,
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
                        Endpoint = normalizedEndpoint,
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
