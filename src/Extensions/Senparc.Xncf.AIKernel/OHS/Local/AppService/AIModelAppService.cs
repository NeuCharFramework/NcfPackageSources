using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Utility;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.NeuChar.App.AppStore.Api;
using Senparc.Ncf.Core.Cache;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.AIKernel.Domain.Models.Extensions;
using Senparc.NeuChar.App.AppStore;


namespace Senparc.Xncf.AIKernel.OHS.Local.AppService
{
    //[BackendJwtAuthorize]
    //TODO: 需要权限验证
    public class AIModelAppService : AppServiceBase
    {
        private readonly AIModelService _aIModelService;


        public AIModelAppService(
            IServiceProvider serviceProvider,
            AIModelService aIModelService) : base(serviceProvider)
        {
            _aIModelService = aIModelService;
        }

        protected virtual Expression<Func<AIModel, bool>> GetListWhere(AIModel_GetListRequest request)
        {
            SenparcExpressionHelper<AIModel> helper = new();
            helper.ValueCompare
                .AndAlso(!request.Alias.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.Alias, request.Alias))
                .AndAlso(!request.DeploymentName.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.DeploymentName, request.DeploymentName))
                .AndAlso(!request.Endpoint.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.Endpoint, request.Endpoint))
                .AndAlso(!request.OrganizationId.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.OrganizationId, request.OrganizationId))
                .AndAlso(request.Show != null, z => z.Show == request.Show)
                ;
            return helper.BuildWhereExpression();
        }

        /// <summary>
        /// AIModel
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AIModelDto>> GetAsync(int id)
        {
            return await this.GetResponseAsync<AIModelDto>(async (response, logger) =>
            {
                var aIModel = await _aIModelService.GetObjectAsync(z => z.Id == id);
                return _aIModelService.Mapper.Map<AIModelDto>(aIModel);
            });
        }

        /// <summary>
        /// 分页获取AIModel
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PagedResponse<AIModelDto>>> GetPagedListAsync(AIModel_GetListRequest request)
        {
            return await this
                .GetResponseAsync<AppResponseBase<PagedResponse<AIModelDto>>, PagedResponse<AIModelDto>>(
                    async (response, logger) =>
                    {
                        var where = GetListWhere(request);

                        var modelList = await _aIModelService.GetObjectListAsync(request.Page, request.Size, where, request.Order);

                        var total = await _aIModelService.GetCountAsync(where);

                        return new PagedResponse<AIModelDto>(
                            total,
                            modelList.Select(m => new AIModelDto(m))
                        );
                    });
        }

        /// <summary>
        /// 获取AIModel列表
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<AIModelDto>>> GetListAsync(AIModel_GetListRequest request)
        {
            return await this.GetResponseAsync<List<AIModelDto>>(
                async (response, logger) =>
                {
                    var where = this.GetListWhere(request);

                    var modelList = (await _aIModelService.GetFullListAsync(where, request.Order))
                        .Select(m => new AIModelDto(m))
                        .ToList();

                    return modelList;
                });
        }

        /// <summary>
        /// 新建一个AIModel
        /// </summary>
        /// <param name="createRequest"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AIModelDto>> CreateAsync(AIModel_CreateOrEditRequest createRequest)
        {
            return await this.GetResponseAsync<AIModelDto>(
                async (response, logger) =>
                {
                    // #region Validate
                    //
                    // var count = await _aIModelService.GetCountAsync(
                    //     z => z.DeploymentName == createRequest.DeploymentName
                    // );
                    // if (count > 0)
                    // {
                    //     //response.ErrorMessage = "AIModel已存在";
                    //     //response.Success = false;
                    //     //return null;
                    //     throw new NcfExceptionBase("AIModel已存在");
                    // }
                    //
                    // #endregion

                    return await _aIModelService.AddAsync(createRequest);
                });
        }

        /// <summary>
        /// 修改AIModel
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AIModelDto>> EditAsync(AIModel_CreateOrEditRequest request)
        {
            return await this.GetResponseAsync<AIModelDto>(async (response, logger) =>
            {
                var aiModelDto = await _aIModelService.EditAsync(request);

                return _aIModelService.Mapper.Map<AIModelDto>(aiModelDto);
            });
        }

        /// <summary>
        /// 删除AIModel
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<AppResponseBase<bool>> DeleteAsync(int id)
        {
            return await this.GetResponseAsync<bool>(async (response, logger) =>
            {
                AIModel aIModel = await _aIModelService.GetObjectAsync(z => z.Id == id);
                if (aIModel == null)
                {
                    response.ErrorMessage = "当前实体已删除或不存在!";
                    response.Success = false;
                    return false;
                }

                await _aIModelService.DeleteObjectAsync(aIModel);
                return true;
            });
        }

        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<string>> UpdateNeuCharModels(AIModel_UpdateNeuCharModelsRequest request)
        {
            return await this.GetResponseAsync<string>(async (r, l) =>
            {
                FullSystemConfigCache fullSystemConfigCache = base.ServiceProvider.GetService<FullSystemConfigCache>();
                var fullSystemConfig = fullSystemConfigCache.Data;

                if (fullSystemConfig.NeuCharAppKey.IsNullOrEmpty() || fullSystemConfig.NeuCharAppSecret.IsNullOrEmpty())
                {
                    r.Success = false;
                    //TODO: 使用日志下载提供详细教程
                    r.ErrorMessage= "错误：当前系统未配置 NeuChar 开发者账号，请到【系统管理】模块，设置页面，使用【更新 NeuChar 云账户信息】绑定 NeuChar 开发者账号！";
                    return "error";
                }

                //var apiContainer = new ApiContainer(base.ServiceProvider, fullSystemConfig.NeuCharAppKey, fullSystemConfig.NeuCharAppSecret);

                ////TODO: 集成到 ApiContainer
                ///


                //var url = $"{Senparc.NeuChar.App.AppStore.Config.DefaultDomainName}/api/developer/getModelInfoByDeveloper";
                //var data = new Dictionary<string, string>() { { "accesstoken", apiContainer.Passport.Token } };

                var passportUrl = $"{Senparc.NeuChar.App.AppStore.Config.DefaultDomainName}/App/Api/GetPassport";
                //Console.WriteLine("passport:" + (passportUrl));

                var data = new Dictionary<string, string>() {
                    { "appKey",fullSystemConfig.NeuCharAppKey },
                    { "secret" ,fullSystemConfig.NeuCharAppSecret}
                  };

                var passportResult = await Senparc.CO2NET.HttpUtility.Post.PostFileGetJsonAsync<PassportResult>(base.ServiceProvider, passportUrl, postDataDictionary: data, encoding: Encoding.UTF8);

                if (passportResult.Result != AppResultKind.成功)
                {
                    r.Success = false;
                    r.ErrorMessage = "AppKey 或 AppSecret 错误！请重新设置！";
                    return "error";
                }

                if (request.DeveloperId != passportResult.Data.DeveloperId)
                {
                    r.Success = false;
                    r.ErrorMessage= "所提供和 DeveloperId 和系统配置的 AppKey 不一致，请检查或更新后重试！";
                    return "error";
                }

                var url = $"{Senparc.NeuChar.App.AppStore.Config.DefaultDomainName}/api/developer/getModelInfoByDeveloper";

                var modelData = new Dictionary<string, string>() 
                {
                    { "accessToken",passportResult.Data.Token }
                };

                var models = await Senparc.CO2NET.HttpUtility.Post.PostGetJsonAsync<NeuCharGetModelJsonResult>(base.ServiceProvider, url, formData: modelData, encoding: Encoding.UTF8);

                //TODO: 核验 AppKey 是否正确

                var result = await _aIModelService.UpdateModelsFromNeuCharAsync(models, passportResult.Data.DeveloperId, request.ApiKey);

                //TODO：立即使用一个模型做一个测试

                return "更新成功！";
            });
        }
    }
}