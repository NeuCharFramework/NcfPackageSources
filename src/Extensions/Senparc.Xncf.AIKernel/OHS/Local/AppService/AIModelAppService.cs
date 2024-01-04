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


namespace Senparc.Xncf.AIKernel.OHS.Local.AppService
{
    //[BackendJwtAuthorize]
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
            SenparcExpressionHelper<AIModel> helper = new ();
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
            return await this.GetResponseAsync<AppResponseBase<AIModelDto>, AIModelDto>(async (response, logger) =>
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
            return await this.GetResponseAsync<AppResponseBase<List<AIModelDto>>, List<AIModelDto>>(
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
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AIModelDto>> CreateAsync(AIModel_CreateRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AIModelDto>, AIModelDto>(async (response, logger) =>
            {
                #region Validate

                var count = await _aIModelService.GetCountAsync(
                    z => z.DeploymentName == request.DeploymentName
                );
                if (count > 0)
                {
                    //response.ErrorMessage = "AIModel已存在";
                    //response.Success = false;
                    //return null;
                    throw new NcfExceptionBase("AIModel已存在");
                }

                #endregion
                
                return await _aIModelService.AddAsync(request);
            });
        }

        /// <summary>
        /// 修改AIModel
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AIModelDto>> EditAsync(AIModel_EditRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AIModelDto>, AIModelDto>(async (response, logger) =>
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
            return await this.GetResponseAsync<AppResponseBase<bool>, bool>(async (response, logger) =>
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
    }
}