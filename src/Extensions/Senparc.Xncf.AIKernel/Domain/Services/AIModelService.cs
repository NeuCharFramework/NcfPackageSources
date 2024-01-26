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
        public SenparcAiSetting BuildSenparcAiSetting(AIModelDto aiModel)
        {
            var aiSettings = new SenparcAiSetting
            {
                AiPlatform = aiModel.AiPlatform
            };

            switch (aiSettings.AiPlatform)
            {
                case AiPlatform.NeuCharAI:
                    aiSettings.NeuCharAIKeys = new NeuCharAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        NeuCharAIApiVersion = aiModel.ApiVersion, // SK中实际上没有用ApiVersion
                        NeuCharEndpoint = aiModel.Endpoint
                    };
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        AzureOpenAIApiVersion = aiModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = aiModel.Endpoint
                    };
                    break;
                case AiPlatform.AzureOpenAI:
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        AzureOpenAIApiVersion = aiModel.ApiVersion, // SK中实际上没有用ApiVersion
                        AzureEndpoint = aiModel.Endpoint
                    };
                    break;
                case AiPlatform.HuggingFace:
                    aiSettings.HuggingFaceKeys = new HuggingFaceKeys()
                    {
                        Endpoint = aiModel.Endpoint
                    };
                    break;
                case AiPlatform.OpenAI:
                    aiSettings.OpenAIKeys = new OpenAIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        OrganizationId = aiModel.OrganizationId
                    };
                    break;
                case AiPlatform.FastAPI:
                    aiSettings.FastAPIKeys = new FastAPIKeys()
                    {
                        ApiKey = aiModel.ApiKey,
                        Endpoint = aiModel.Endpoint,
                        //OrganizationId = aiModel.OrganizationId
                    };
                    break;
                default:
                    throw new NcfExceptionBase($"暂时不支持{aiSettings.AiPlatform}类型");
            }


            return aiSettings;
        }

    }
}