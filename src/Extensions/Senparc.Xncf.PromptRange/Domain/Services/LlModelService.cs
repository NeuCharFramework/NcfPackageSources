/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlModelService.cs
    File description: LlModelService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Threading.Tasks;
using Senparc.AI;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.AIKernel.Domain.Models;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class LlModelService : ServiceBase<LlModel>
    {
        /// <summary>Initializes LLM model persistence.</summary>
        /// <param name="repo">The LLM repository.</param>
        /// <param name="serviceProvider">The dependency provider.</param>
        public LlModelService(IRepositoryBase<LlModel> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>Loads one LLM model DTO by identifier.</summary>
        /// <param name="llmId">The model identifier.</param>
        /// <returns>The model DTO.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the model is missing.</exception>
        public async Task<LlModelDto> GetLlmModelById(int llmId)
        {
            var model = await base.GetObjectAsync(n => n.Id == llmId) ??
                        throw new NcfExceptionBase($"找不到{llmId}对应的模型");

            return this.Mapper.Map<LlModelDto>(model);
        }

        /// <summary>Validates and creates an enabled LLM model.</summary>
        /// <param name="request">The model creation request.</param>
        /// <returns>The created model DTO.</returns>
        public async Task<LlModelDto> AddAsync(LlModel_AddRequest request)
        {
            #region validate

            //Legacy comment translated to English.
            // if (request.ModelType == AI.AiPlatform.AzureOpenAI.ToString() || request.ModelType == AI.AiPlatform.NeuCharAI.ToString())

            if (request.ModelType is AiPlatform.AzureOpenAI)
            {
                //Legacy comment translated to English.
                if (string.IsNullOrWhiteSpace(request.ApiVersion) || string.IsNullOrWhiteSpace(request.Endpoint))
                {
                    throw new NcfExceptionBase("使用AzureOpenAI时，ApiVersion和Endpoint不能为空");
                }

                //Legacy comment translated to English.
                if (!string.IsNullOrWhiteSpace(request.ApiVersion) && !Constants.ApiVersionList.Contains(request.ApiVersion))
                {
                    throw new NcfExceptionBase("ApiVersion不存在");
                }
            }

            #endregion

            LlModel model = new LlModel(request);

            model.Switch(true);

            await this.SaveObjectAsync(model);

            return this.Mapper.Map<LlModelDto>(model);
        }

        /// <summary>Updates an existing LLM model's editable fields.</summary>
        /// <param name="request">The model update request.</param>
        /// <returns>The updated model DTO.</returns>
        public async Task<LlModelDto> UpdateAsync(LlmModel_ModifyRequest request)
        {
            var model = await this.GetObjectAsync(m => m.Id == request.Id) ??
                        throw new NcfExceptionBase("未找到该模型");

            model.Update(request.Alias, request.Show, request.DeploymentName);

            await this.SaveObjectAsync(model);

            return this.Mapper.Map<LlModelDto>(model);
        }

        /// <summary>Maps a PromptRange configuration-model type to an AI platform model type.</summary>
        /// <param name="configModelType">The PromptRange model type.</param>
        /// <returns>The mapped configuration model.</returns>
        public ConfigModel ConvertToConfigModel(ConfigModelType configModelType)
        {
            ConfigModel configModel;

            switch (configModelType)
            {
                case AIKernel.Domain.Models.ConfigModelType.TextCompletion:
                    configModel = ConfigModel.TextCompletion;
                    break;
                case AIKernel.Domain.Models.ConfigModelType.Chat:
                    configModel = ConfigModel.Chat;
                    break;
                case AIKernel.Domain.Models.ConfigModelType.TextToImage:
                    configModel = ConfigModel.TextToImage;
                    //Legacy comment translated to English.
                    break;
                case AIKernel.Domain.Models.ConfigModelType.TextEmbedding:
                case AIKernel.Domain.Models.ConfigModelType.ImageToText:
                case AIKernel.Domain.Models.ConfigModelType.TextToSpeech:
                case AIKernel.Domain.Models.ConfigModelType.SpeechToText:
                case AIKernel.Domain.Models.ConfigModelType.SpeechRecognition:
                default:
                    configModel = ConfigModel.TextCompletion;
                    break;
            }

            return configModel;
        }
    }
}
