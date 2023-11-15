using Microsoft.AspNetCore.Mvc;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Repository;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using Senparc.Xncf.PromptRange.OHS.Local.PL.response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Extensions;
using Microsoft.SemanticKernel;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    public class PromptItemAppService : AppServiceBase
    {
        private readonly RepositoryBase<PromptItem> _promptItemRepository;
        private readonly PromptItemService _promptItemService;
        private readonly LlmModelService _llmModelService;
        private readonly PromptResultService _promptResultService;

        public PromptItemAppService(IServiceProvider serviceProvider,
            RepositoryBase<PromptItem> promptItemRepository,
            PromptItemService promptItemService,
            LlmModelService llmModelService,
            PromptResultService promptResultService) : base(serviceProvider)
        {
            _promptItemRepository = promptItemRepository;
            _promptItemService = promptItemService;
            _llmModelService = llmModelService;
            _promptResultService = promptResultService;
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptResultDto>> Add(PromptItem_AddRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptResultDto>, PromptResultDto>(
                async (response, logger) =>
                {
                    //validate request dto
                    var promptItem = await _promptItemService.AddPromptItemAsync(request);

                    #region AI 调用

                    var llmModel = await _llmModelService.GetObjectAsync(z => z.Id == request.ModelId);
                    if (llmModel == null)
                    {
                        throw new NcfExceptionBase($"未找到模型{request.ModelId}");
                    }

                    var userId = "Test";
                    var modelName = LlmModelHelper.GetAzureModelName(llmModel.Name);
                    var aiSettings = this.BuildSenparcAiSetting(llmModel);
                    //创建 AI Handler 处理器（也可以通过工厂依赖注入）
                    var handler = new SemanticAiHandler(new AI.Kernel.Helpers.SemanticKernelHelper(aiSettings));

                    //定义 AI 接口调用参数和 Token 限制等
                    var promptParameter = new PromptConfigParameter()
                    {
                        MaxTokens = request.MaxToken > 0 ? request.MaxToken : 2000,
                        Temperature = request.Temperature,
                        TopP = request.TopP,
                        FrequencyPenalty = request.FrequencyPenalty,
                        PresencePenalty = request.PresencePenalty
                    };

                    var functionPrompt = @"请根据提示输出对应内容：\n{{input}}";

                    // var kernelBuilder = helper.ConfigTextCompletion(userId, modelName, aiSettings);
                    // kernelBuilder.WithAzureTextCompletionService(llmModel.Name,llmModel.Endpoint,llmModel.ApiKey);
                    var iWantToRun = handler.IWantTo()
                        .ConfigModel(ConfigModel.TextCompletion, userId, modelName, aiSettings)
                        .BuildKernel()
                        .RegisterSemanticFunction("TestPrompt", "PromptRange", promptParameter)
                        .iWantToRun;


                    // //todo which function to use? completion or chat?
                    // var func = "chat/completions";
                    // // var func = "completions";
                    //
                    //
                    // // construct the target url
                    // // 枚举Constants.ModelTypeEnum, 生成对应的url
                    // string aiUrl;
                    // if (Enum.TryParse(llmModel.ModelType, out AiPlatform aiPlatform))
                    // {
                    //   switch (aiPlatform)
                    //   {
                    //     case AiPlatform.AzureOpenAI:
                    //       //todo: validate the parameters
                    //       aiUrl = $"{llmModel.Endpoint}/{modelName}/{func}?api-version={llmModel.ApiVersion}";
                    //       break;
                    //     case AiPlatform.HuggingFace:
                    //       aiUrl = $"{llmModel.Endpoint}/{func}?api-version={llmModel.ApiVersion}";
                    //       break;
                    //     case AiPlatform.OpenAI:
                    //       aiUrl = $"https://api.openai.com/v1/{func}";
                    //       break;
                    //     default: //暂时不支持其他的
                    //       aiUrl = "";
                    //       logger.Append("未知的模型类型");
                    //       break;
                    //   }
                    // }
                    // else
                    // {
                    //   aiUrl = "";
                    //   logger.Append("未知的模型类型");
                    // }

                    var context = iWantToRun.CreateNewContext();
                    context.context.Variables["input"] = request.Content;

                    var aiRequest = iWantToRun.CreateRequest(context.context.Variables, true);
                    var result = await iWantToRun.RunAsync(aiRequest);

                    var dt1 = SystemTime.Now;

                    var promptCostToken = 0;
                    var resultCostToken = 0;

                    var promptResult = new PromptResult(
                        request.ModelId, result.Output, SystemTime.DiffTotalMS(dt1), 
                        0, 0, null, false, TestType.Text,
                        promptCostToken, resultCostToken, promptCostToken + resultCostToken,
                        promptItem.Version, promptItem.Id);

                    await _promptResultService.SaveObjectAsync(promptResult);

                    #endregion

                    return _promptResultService.Mapper.Map<PromptResultDto>(promptResult);
                });
        }

        private SenparcAiSetting BuildSenparcAiSetting(LlmModel llmModel)
        {
            var aiSettings = new SenparcAiSetting();

            if (!Enum.TryParse(llmModel.ModelType, out AiPlatform aiPlatform))
                throw new Exception("无法转换为AiPlatform");
            aiSettings.AiPlatform = aiPlatform;
            switch (aiPlatform)
            {
                case AiPlatform.AzureOpenAI:
                    aiSettings.AzureOpenAIKeys = new AzureOpenAIKeys()
                    {
                        ApiKey = llmModel.ApiKey,
                        AzureOpenAIApiVersion = llmModel.ApiVersion,
                        AzureEndpoint = llmModel.Endpoint
                    };
                    break;
                case AiPlatform.HuggingFace:
                    aiSettings.HuggingFaceKeys = new HuggingFaceKeys()
                    {
                        Endpoint = llmModel.Endpoint
                    };
                    break;
                case AiPlatform.OpenAI:
                    aiSettings.OpenAIKeys = new OpenAIKeys()
                    {
                        ApiKey = llmModel.ApiKey,
                        OrganizationId = llmModel.OrganizationId
                    };
                    break;
                default:
                    break;
            }


            return aiSettings;
        }

        [ApiBind]
        public async Task<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>> GetIdAndName()
        {
            return await
                this.GetResponseAsync<AppResponseBase<List<PromptItem_GetIdAndNameResponse>>,
                    List<PromptItem_GetIdAndNameResponse>>(async (response, logger) =>
                {
                    return (await _promptItemService.GetFullListAsync(p => true, p => p.Id,
                        Ncf.Core.Enums.OrderingType.Ascending)).Select(p => new PromptItem_GetIdAndNameResponse
                    {
                        Id = p.Id,
                        Name = p.Name
                    }).ToList();
                });
        }

        [ApiBind]
        public async Task<AppResponseBase<PromptItem_GetResponse>> Get(int id)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptItem_GetResponse>, PromptItem_GetResponse>(
                async (response, logger) =>
                {
                    var prompt = await _promptItemService.GetObjectAsync(p => p.Id == id);
                    var model = await _llmModelService.GetObjectAsync(p => p.Id == prompt.ModelId);
                    var result = await _promptResultService.GetObjectAsync(p => p.PromptItemId == prompt.Id);

                    return new PromptItem_GetResponse()
                    {
                        PromptName = prompt.Name,
                        Show = prompt.Show,
                        Version = prompt.Version,
                        ModelName = model.Name,
                        ResultString = result.ResultString,
                        Score = Convert.ToInt32(result.RobotScore > 0 ? result.RobotScore : result.HumanScore),
                        Time = prompt.AddTime.ToString("yyyy-MM-dd")
                    };
                });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> Scoring(PromptItem_ScoringRequest req)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var result = await _promptResultService.GetObjectAsync(p => p.PromptItemId == req.PromptItemId);

                result.Scoring(req.HumanScore);

                await _promptResultService.SaveObjectAsync(result);

                return "ok";
            });
        }

        [ApiBind]
        public async Task<AppResponseBase<List<PromptItem_GetResponse>>> GetVersionInfoList()
        {
            return await this
                .GetResponseAsync<AppResponseBase<List<PromptItem_GetResponse>>, List<PromptItem_GetResponse>>(
                    async (response, logger) =>
                    {
                        var result = await _promptItemService.GetFullListAsync(p => true, p => p.Id,
                            Ncf.Core.Enums.OrderingType.Descending);

                        List<int> modelIdList = result.Select(p => p.ModelId).Distinct().ToList();

                        var modelList = await _llmModelService.GetFullListAsync(p => modelIdList.Contains(p.Id),
                            p => p.Id, Ncf.Core.Enums.OrderingType.Descending);

                        List<PromptItem_GetResponse> list = new List<PromptItem_GetResponse>();
                        foreach (var item in result)
                        {
                            list.Add(new PromptItem_GetResponse()
                            {
                                Version = item.Version,
                                Time = item.AddTime.ToString("yyyy-MM-dd"),
                                ModelName = modelList.FirstOrDefault(p => p.Id == item.ModelId)?.Name,
                                PromptName = item.Name,
                            });
                        }

                        return list;
                    });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> Modify(PromptItem_ModifyRequest req)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var result = await _promptItemService.GetObjectAsync(p => p.Id == req.Id) ??
                             throw new Exception("未找到prompt");

                result.ModifyName(req.Name);

                await _promptItemService.SaveObjectAsync(result);

                return "ok";
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> Del([FromQuery] int id)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var result = await _promptItemService.GetObjectAsync(p => p.Id == id) ??
                             throw new Exception("未找到prompt");

                await _promptItemService.DeleteObjectAsync(result);

                return "ok";
            });
        }
    }
}