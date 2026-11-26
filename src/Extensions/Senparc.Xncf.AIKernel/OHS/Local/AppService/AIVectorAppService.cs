/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIVectorAppService.cs
    File description: AIVectorAppService related implementation

    Created by: Senparc - 20250402

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

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
using Senparc.Xncf.AreaBase.Admin.Filters;


namespace Senparc.Xncf.AIKernel.OHS.Local.AppService
{

    [ApiAuthorize]
    //TODO: permission validation is required
    public class AIVectorAppService : AppServiceBase
    {
        private readonly AIVectorService _aIVectorService;


        /// <summary>
        /// Initializes AI vector administration operations.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="aIVectorService">The vector persistence service.</param>
        public AIVectorAppService(
            IServiceProvider serviceProvider,
            AIVectorService aIVectorService) : base(serviceProvider)
        {
            _aIVectorService = aIVectorService;
        }

        /// <summary>
        /// Builds the vector list predicate; the current implementation returns an unrestricted predicate.
        /// </summary>
        /// <param name="request">The vector filter request.</param>
        /// <returns>An EF-translatable vector predicate.</returns>
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
        /// Loads one vector database configuration.
        /// </summary>
        /// <param name="id">The vector configuration identifier.</param>
        /// <returns>A response containing the vector DTO, or an empty mapping when absent.</returns>
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
        /// Returns a paged vector database list using the request filter and ordering.
        /// </summary>
        /// <param name="request">The paging, filtering, and ordering request.</param>
        /// <returns>A response containing vector DTOs and total count.</returns>
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
        /// Returns a paged vector database selection list for model/configuration pickers.
        /// </summary>
        /// <param name="request">The paging, filtering, and ordering request.</param>
        /// <returns>A response containing selection DTOs and total count.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PagedResponse<AIVectorSelectionResponse>>> GetSelectionListAsync(AIVector_GetListRequest request)
        {
            return await this
                .GetResponseAsync<AppResponseBase<PagedResponse<AIVectorSelectionResponse>>, PagedResponse<AIVectorSelectionResponse>>(
                    async (response, logger) =>
                    {
                        var where = GetListWhere(request);

                        var vectorList = await _aIVectorService.GetObjectListAsync(request.Page, request.Size, where, request.Order);
                        var total = await _aIVectorService.GetCountAsync(where);

                        return new PagedResponse<AIVectorSelectionResponse>(
                            total,
                            vectorList.Select(m => new AIVectorSelectionResponse(m)));
                    });
        }

        /// <summary>
        /// Returns all vector database configurations matching the supplied filter.
        /// </summary>
        /// <param name="request">The filter and ordering request.</param>
        /// <returns>A response containing vector DTOs.</returns>
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
        /// Creates and persists a vector database configuration.
        /// </summary>
        /// <param name="createRequest">The vector provider, connection, alias, and metadata.</param>
        /// <returns>A response containing the created vector DTO.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the vector configuration is invalid.</exception>
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
                    // The duplicate-deployment validation remains disabled until the
                    // provider-specific uniqueness rule is defined.
                    //     //response.Success = false;
                    //     //return null;
                    // Keep the historical validation block available for a future
                    // provider-aware uniqueness check without changing this endpoint's behavior.
                    // }
                    //
                    // #endregion

                    return await _aIVectorService.AddAsync(createRequest);
                });
        }

        /// <summary>
        /// Updates an existing vector database configuration.
        /// </summary>
        /// <param name="request">The vector identifier and replacement values.</param>
        /// <returns>A response containing the updated vector DTO.</returns>
        /// <exception cref="NcfExceptionBase">Thrown when the vector does not exist or is invalid.</exception>
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
        /// Deletes one vector database configuration.
        /// </summary>
        /// <param name="id">The vector configuration identifier.</param>
        /// <returns>A response indicating whether deletion completed.</returns>
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
