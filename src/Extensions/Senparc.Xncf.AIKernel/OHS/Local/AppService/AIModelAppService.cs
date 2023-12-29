
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
using Senparc.Xncf.AIKernel.Models;


namespace Senparc.Xncf.AIKernel.OHS.Local.AppService
{

	//[BackendJwtAuthorize]
	public class AIModelAppService : AppServiceBase
	{
		private readonly AIModelService _aIModelService;


        public AIModelAppService(IServiceProvider serviceProvider, AIModelService aIModelService ) : base(serviceProvider)
        {
            _aIModelService = aIModelService;
  
        }
        protected virtual Expression<Func<AIModel,bool>> GetListWhere(AIModel_GetListRequest request)
        {
			SenparcExpressionHelper<AIModel> helper = new SenparcExpressionHelper<AIModel>();
			helper.ValueCompare
                        .AndAlso(!request.Name.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.Name, request.Name)) 
                        .AndAlso(!request.Endpoint.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.Endpoint, request.Endpoint))  
                        .AndAlso(!request.OrganizationId.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.OrganizationId, request.OrganizationId)) 
                        .AndAlso(!request.ApiKey.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.ApiKey, request.ApiKey)) 
                        .AndAlso(!request.ApiVersion.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.ApiVersion, request.ApiVersion)) 
                        .AndAlso(!request.Note.IsNullOrWhiteSpace(), z => EF.Functions.Like(z.Note, request.Note))   
						;
			return helper.BuildWhereExpression();
		}
        /// <summary>
        /// AIModel
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AIModel_Response>> GetAsync(int id)
        {
            return await this.GetResponseAsync<AppResponseBase<AIModel_Response>, AIModel_Response>(async (response, logger) =>
            {
                var aIModel = await _aIModelService.GetObjectAsync(z => z.Id==id);
                return _aIModelService.Mapper.Map<AIModel_Response>(aIModel);
            });
        }
        /// <summary>
        /// 分页获取AIModel
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
		public async Task<AppResponseBase<PagedResponse<AIModel_Response>>> GetListAsync(AIModel_GetListRequest request)
		{
			return await this.GetResponseAsync<AppResponseBase<PagedResponse<AIModel_Response>>, PagedResponse <AIModel_Response>> (async (response, logger) =>
			{

                var where = GetListWhere(request);
				var items = await _aIModelService.GetObjectListAsync(request.Page,request.Size, where, request.Order);
				var total = await _aIModelService.GetCountAsync(where);
				return new PagedResponse<AIModel_Response>( _aIModelService.Mapper.Map<List<AIModel_Response>>(items),total);
			});
		}
        /// <summary>
        /// 新建一个AIModel
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod =  ApiRequestMethod.Post)]
		public async Task<AppResponseBase<bool>> CreateAsync(AIModel_CreateOrEditRequest request)
		{
			return await this.GetResponseAsync<AppResponseBase<bool>, bool>(async (response, logger) =>
			{
                if(await _aIModelService.GetCountAsync(z=>z.Name== request.Name) > 0)
                {
                    response.ErrorMessage = "AIModel已存在";
                    response.Success = false;
                    return false;
                }
				var aIModel= _aIModelService.Mapper.Map<AIModel>(request);
               
				await _aIModelService.SaveObjectAsync(aIModel);
				return true;
			});
		}
        /// <summary>
        /// 修改AIModel
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
		public async Task<AppResponseBase<AIModel_Response>> EditAsync(int id, AIModel_CreateOrEditRequest request)
		{
			return await this.GetResponseAsync<AppResponseBase<AIModel_Response>, AIModel_Response>(async (response, logger) =>
			{
				AIModel aIModel = await _aIModelService.GetObjectAsync(z=>z.Id==id);
				if (aIModel == null)
				{
					response.ErrorMessage = "未查询到实体!";
					response.Success = false;
					return null;
				}
				_aIModelService.Mapper.Map( request,aIModel);
				await _aIModelService.SaveObjectAsync(aIModel);
				return _aIModelService.Mapper.Map<AIModel_Response>(aIModel);
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

