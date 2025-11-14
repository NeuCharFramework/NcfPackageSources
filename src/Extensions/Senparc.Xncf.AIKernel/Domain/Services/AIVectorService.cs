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
using Senparc.AI.Kernel;
using Senparc.AI;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Kernel.Handlers;
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

            #region 如果字段为空就不更新

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
        ///// 构造 SenparcAiSetting
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
        //                throw new Exception($"尚未支持：{aiModel.ConfigModelType} 模型在 BuildSenparcAiSetting 中的处理");
        //        }
        //        return modelName;
        //    };

        //    var modelName = GetModelName();



        //    return aiSettings;
        //}

        ///// <summary>
        ///// 运行模型
        ///// </summary>
        ///// <param name="senparcAiSetting"></param>
        ///// <param name="prompt"></param>
        ///// <returns></returns>
        //public async Task<SenparcKernelAiResult<string>> RunModelsync(SenparcAiSetting senparcAiSetting, string prompt, string systemMessage, string promptTemplate, PromptConfigParameter promptConfigParameter=null)
        //{
        //    if (senparcAiSetting == null)
        //    {
        //        throw new SenparcAiException("SenparcAiSetting 不能为空");
        //    }

        //    promptConfigParameter ??= new PromptConfigParameter()
        //    {
        //        MaxTokens = 2000,
        //        Temperature = 0.7,
        //        TopP = 0.5,
        //    };

        //    var semanticAiHandler = base._serviceProvider.GetService<SemanticAiHandler>();
        //    var chatConfig = semanticAiHandler.ChatConfig(promptConfigParameter, userId: "Jeffrey",
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
        //        return "模型数据不存在，请检查是否已部署，或是否具备权限！";
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
        //            Note = $"从 NeuChar AI 导入（DevId:{developerId}）",
        //            Show = true
        //        };

        //        //TODO: 远程不提供，临时本地判断
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
        //    return $"已成功添加 {addCount} 个模型，更新 {updateCount} 个模型信息。";
        //}
    }
}