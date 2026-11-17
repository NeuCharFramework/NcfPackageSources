/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: AIVectorService.cs
    File description: Related implementation.
    
    
    Created by: Senparc - 20250402
    
    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using Senparc.AI.AgentKernel;
using Senparc.AI;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.AI.Entities;
using Senparc.AI.Exceptions;
using Senparc.AI.Entities.Keys;
using Senparc.Xncf.AIKernel.Domain.Models.Extensions;
using Senparc.CO2NET.Extensions;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    public class AIVectorService : ServiceBase<AIVector>
    {
        public AIVectorService(IRepositoryBase<AIVector> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        public async Task<AIVectorDto> AddAsync(AIVector_CreateOrEditRequest orEditRequest)
        {
            AIVector aiVector = new AIVector(orEditRequest);
            // var aIModel = _aIModelService.Mapper.Map<AIModel>(request);

            aiVector.SwitchShow(true);

            await this.SaveObjectAsync(aiVector);

            var aiVectorDto = new AIVectorDto(aiVector);

            return aiVectorDto;
        }

        public async Task<AIVectorDto> EditAsync(AIVector_CreateOrEditRequest request)
        {
            AIVector aiVector = await this.GetObjectAsync(z => z.Id == request.Id)
                              ?? throw new NcfExceptionBase("未查询到实体!");

            #region Legacy section

            //if (string.IsNullOrWhiteSpace(request.ApiKey))
            //{
            //    request.ApiKey = aiVector.ApiKey;
            //}
            //if (string.IsNullOrWhiteSpace(request.OrganizationId))
            //{
            //    request.OrganizationId = aiVector.OrganizationId;
            //}

            #endregion

            aiVector.Update(request);

            await this.SaveObjectAsync(aiVector);

            var aiVectorDto = new AIVectorDto(aiVector);

            return aiVectorDto;
        }

        ///// <summary>
        ///// build SenparcAiSetting
        ///// </summary>
        ///// <param name="aiVector"></param>
        ///// <returns></returns>
        ///// <exception cref="NcfExceptionBase"></exception>
        //public SenparcAiSetting BuildSenparcAiSetting(AIVectorDto aiVector)
        //{
        //    var aiSettings = new SenparcAiSetting
        //    {
        //        AiPlatform = aiVector.AiPlatform
        //    };

        //    Func<ModelName> GetModelName = () =>
        //    {
        //        ModelName modelName = new();
        //        switch (aiModel.ConfigModelType)
        //        {
        //            case Models.ConfigModelType.TextCompletion:
        //                modelName.TextCompletion = aiModel.ModelId;
        //                break;
        //            case Models.ConfigModelType.Chat:
        //                modelName.Chat = aiModel.ModelId;
        //                break;
        //            case Models.ConfigModelType.TextEmbedding:
        //                modelName.Embedding = aiModel.ModelId;
        //                break;
        //            case Models.ConfigModelType.TextToImage:
        //                modelName.TextToImage = aiModel.ModelId;
        //                break;
        //            case Models.ConfigModelType.ImageToText:
        //            case Models.ConfigModelType.TextToSpeech:
        //            case Models.ConfigModelType.SpeechToText:
        //            case Models.ConfigModelType.SpeechRecognition:
        //            default:
        //Legacy comment translated to English.
        //        }
        //        return modelName;
        //    };

        //    var modelName = GetModelName();



        //    return aiSettings;
        //}

        ///// <summary>
        ///// runmodel
        ///// </summary>
        ///// <param name="senparcAiSetting"></param>
        ///// <param name="prompt"></param>
        ///// <returns></returns>
        //public async Task<SenparcKernelAiResult<string>> RunModelsync(SenparcAiSetting senparcAiSetting, string prompt, string systemMessage, string promptTemplate, PromptConfigParameter promptConfigParameter=null)
        //{
        //    if (senparcAiSetting == null)
        //    {
        //Legacy comment translated to English.
        //    }

        //    promptConfigParameter ??= new PromptConfigParameter()
        //    {
        //        MaxTokens = 2000,
        //        Temperature = 0.7,
        //        TopP = 0.5,
        //    };

        //    var agentAiHandler = base._serviceProvider.GetService<AgentAiHandler>();
        //    var chatConfig = agentAiHandler.ChatConfig(promptConfigParameter, userId: "Jeffrey",
        //         chatSystemMessage: systemMessage, promptTemplate: promptTemplate,
        //         maxHistoryStore: 20, senparcAiSetting: senparcAiSetting);
        //    var iWantToRun = chatConfig;

        //    var request = iWantToRun.CreateRequest(prompt);
        //    var aiResult = await iWantToRun.RunAsync(request);
        //    return aiResult;
        //}

        //public async Task<string> UpdateModelsFromNeuCharAsync(NeuCharGetModelJsonResult modelResult, int developerId, string apiKey)
        //{
        //    if (modelResult?.Result?.Data == null)
        //    {
        //Legacy comment translated to English.
        //    }

        //    var models = await base.GetFullListAsync(z => z.AiPlatform == AiPlatform.NeuCharAI);
        //    var updateCount = 0;
        //    var addCount = 0;
        //    foreach (var neucharModel in modelResult.Result.Data)
        //    {
        //        var model = await base.GetObjectAsync(z => z.DeploymentName == neucharModel.Name);
        //        var dto = new AIModel_CreateOrEditRequest()
        //        {
        //            AiPlatform = AiPlatform.NeuCharAI,
        //            ApiKey = apiKey,
        //            Alias = $"NeuChar-{neucharModel.Name}",
        //            DeploymentName = neucharModel.Name,
        //            ModelId = neucharModel.Name,
        //            ApiVersion = model?.AiPlatform == AiPlatform.AzureOpenAI || model?.AiPlatform == AiPlatform.OpenAI
        //                            ? "2024-05-13"
        //                            : "",
        //            Endpoint = $"https://www.neuchar.com/{developerId}",
        //            ConfigModelType = Models.ConfigModelType.Chat,
        //Legacy comment translated to English.
        //            Show = true
        //        };

        //Legacy comment translated to English.
        //        if (neucharModel.Name.Contains("embedding"))
        //        {
        //            dto.ConfigModelType = Models.ConfigModelType.TextEmbedding;
        //        }
        //        else if (neucharModel.Name.Contains("text-davinci"))
        //        {
        //            dto.ConfigModelType = Models.ConfigModelType.TextCompletion;
        //        }

        //        if (model == null)
        //        {
        //            model = new AIModel(dto);
        //            addCount++;
        //        }
        //        else
        //        {
        //            if (!model.Note.IsNullOrEmpty())
        //            {
        //                dto.Note = model.Note;
        //            }
        //            dto.MaxToken = model.MaxToken;
        //            dto.Alias = model.Alias;
        //            model.Update(dto);

        //            updateCount++;
        //        }

        //        await base.SaveObjectAsync(model);
        //    }
        //Legacy comment translated to English.
        //}
    }
}