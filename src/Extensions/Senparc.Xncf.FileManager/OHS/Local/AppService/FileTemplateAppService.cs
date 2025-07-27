using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using Senparc.Xncf.FileManager.OHS.Local.PL;
using Senparc.Xncf.FileManager.OHS.Local.PL.Response;
using System;
using System.Threading.Tasks;
using Senparc.Xncf.FileManager.Domain;
using Senparc.Xncf.FileManager.Domain.Services;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto;
using System.Linq;


namespace Senparc.Xncf.FileManager.OHS.Local.AppService
{
    public class FileTemplateAppService : AppServiceBase
    {
        private readonly NcfFileService fileService;

        public FileTemplateAppService(IServiceProvider serviceProvider, NcfFileService fileService) : base(serviceProvider)
        {
            this.fileService = fileService;
        }

        /*
         * 使用 [ApiBind] 可将任意方法或类快速创建动态 WebApi。
         * 在 DDD 系统中，出于安全和防腐考虑，建议只在 AppService 上使用。
         * 当 AppService 上添加 [ApiBind] 标签满足不了需求时，仍然可以手动创建 ApiController。
         */

        /// <summary>
        /// 将 AppService 暴露为 WebApi
        /// </summary>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<int>> MyApi()
        {
            return await this.GetResponseAsync<int>(async (response, logger) =>
            {
                await Task.Delay(100);
                return 200;
            });
        }

        /// <summary>
        /// 自定义 Post 类型和复杂参数，同时测试异常抛出和自定义状态码
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> MyCustomApi(Api_MyCustomApiRequest request)
        {
            //StringAppResponse 是 AppResponseBase<string> 的快捷写法
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                throw new NcfExceptionBase($"抛出异常测试，传输参数：{request.FirstName} {request.LastName}");
                response.StateCode = 100;
            },
            exceptionHandler: (ex, response, logger) =>
            {
                logger.Append($"正在处理异常，信息：{ex.Message}");
            },
            afterFunc: (response, logger) =>
            {
                if (response.Success != true)
                {
                    response.StateCode = 101;
                }
            });
        }

        /// <summary>
        /// 获取 AgentTemplate 的列表
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        [ApiBind]
        public async Task<AppResponseBase<FileTemplate_GetListResponse>> GetList(int pageIndex = 0, int pageSize = 0, string filter = "")
        {
            return await this.GetResponseAsync<FileTemplate_GetListResponse>(async (response, logger) =>
            {
                var seh = new SenparcExpressionHelper<Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.NcfFile>();
                seh.ValueCompare.AndAlso(!string.IsNullOrEmpty(filter), _ => _.FileName.Contains(filter));
                var where = seh.BuildWhereExpression();
                var list = await this.fileService.GetObjectListAsync(pageIndex, pageSize, where, z => z.Id, Ncf.Core.Enums.OrderingType.Descending);

                var listDto = new PagedList<NcfFileDto>(list
                    .Select(z =>
                    fileService.Mapping<NcfFileDto>(z)).ToList(),
                        list.PageIndex, list.PageCount, list.TotalCount, list.SkipCount);

                var result = new FileTemplate_GetListResponse()
                {
                    List = listDto
                };
                return result;
            });
        }
    }
}
