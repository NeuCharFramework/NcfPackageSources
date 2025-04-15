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
    public class AIVectorAppService : AppServiceBase
    {
        private readonly AIVectorService _aIVectorService;


        public AIVectorAppService(
            IServiceProvider serviceProvider,
            AIVectorService aIVectorService) : base(serviceProvider)
        {
            _aIVectorService = aIVectorService;
        }

        protected virtual Expression<Func<AIVector, bool>> GetListWhere(AIVector_GetListRequest request)
        {
            SenparcExpressionHelper<AIVector> helper = new();
            //helper.ValueCompare
            //    .AndAlso(!request.Alias.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.Alias, request.Alias))
            //    .AndAlso(!request.DeploymentName.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.DeploymentName, request.DeploymentName))
            //    .AndAlso(!request.Endpoint.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.Endpoint, request.Endpoint))
            //    .AndAlso(!request.OrganizationId.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.OrganizationId, request.OrganizationId))
            //    .AndAlso(request.Show != null, z => z.Show == request.Show)
            //    ;
            return helper.BuildWhereExpression();
        }

        /// <summary>
        /// AIVector
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AIVectorDto>> GetAsync(int id)
        {
            return await this.GetResponseAsync<AIVectorDto>(async (response, logger) =>
            {
                var aIVector = await _aIVectorService.GetObjectAsync(z => z.Id == id);
                return _aIVectorService.Mapper.Map<AIVectorDto>(aIVector);
            });
        }

        /// <summary>
        /// 分页获取AIVector
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PagedResponse<AIVectorDto>>> GetPagedListAsync(AIVector_GetListRequest request)
        {
            return await this
                .GetResponseAsync<AppResponseBase<PagedResponse<AIVectorDto>>, PagedResponse<AIVectorDto>>(
                    async (response, logger) =>
                    {
                        var where = GetListWhere(request);

                        var vectorList = await _aIVectorService.GetObjectListAsync(request.Page, request.Size, where, request.Order);

                        var total = await _aIVectorService.GetCountAsync(where);

                        return new PagedResponse<AIVectorDto>(
                            total,
                            vectorList.Select(m => new AIVectorDto(m))
                        );
                    });
        }

        /// <summary>
        /// 获取AIVector列表
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<AIVectorDto>>> GetListAsync(AIVector_GetListRequest request)
        {
            return await this.GetResponseAsync<List<AIVectorDto>>(
                async (response, logger) =>
                {
                    var where = this.GetListWhere(request);

                    var vectorList = (await _aIVectorService.GetFullListAsync(where, request.Order))
                        .Select(m => new AIVectorDto(m))
                        .ToList();

                    return vectorList;
                });
        }

        /// <summary>
        /// 新建一个AIVector
        /// </summary>
        /// <param name="createRequest"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AIVectorDto>> CreateAsync(AIVector_CreateOrEditRequest createRequest)
        {
            return await this.GetResponseAsync<AIVectorDto>(
                async (response, logger) =>
                {
                    // #region Validate
                    //
                    // var count = await _aIVectorService.GetCountAsync(
                    //     z => z.DeploymentName == createRequest.DeploymentName
                    // );
                    // if (count > 0)
                    // {
                    //     //response.ErrorMessage = "AIVector已存在";
                    //     //response.Success = false;
                    //     //return null;
                    //     throw new NcfExceptionBase("AIVector已存在");
                    // }
                    //
                    // #endregion

                    return await _aIVectorService.AddAsync(createRequest);
                });
        }

        /// <summary>
        /// 修改AIVector
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AIVectorDto>> EditAsync(AIVector_CreateOrEditRequest request)
        {
            return await this.GetResponseAsync<AIVectorDto>(async (response, logger) =>
            {
                var aiVectorDto = await _aIVectorService.EditAsync(request);

                return _aIVectorService.Mapper.Map<AIVectorDto>(aiVectorDto);
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
                AIVector aIVector = await _aIVectorService.GetObjectAsync(z => z.Id == id);
                if (aIVector == null)
                {
                    response.ErrorMessage = "当前实体已删除或不存在!";
                    response.Success = false;
                    return false;
                }

                await _aIVectorService.DeleteObjectAsync(aIVector);
                return true;
            });
        }
    }
}