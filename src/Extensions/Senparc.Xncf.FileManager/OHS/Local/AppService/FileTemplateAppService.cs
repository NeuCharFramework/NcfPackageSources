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
         * Use [ApiBind] to quickly create dynamic WebApi from any method or class.
         * In DDD systems, for security and anti-corrosion considerations, it is recommended to only use AppService.
         * When adding the [ApiBind] tag to the AppService cannot meet the needs, you can still create an ApiController manually.
         */

        /// <summary>
        /// Expose AppService as WebApi
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
        /// Customize Post type and complex parameters, while testing exception throwing and custom status codes
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> MyCustomApi(Api_MyCustomApiRequest request)
        {
            //StringAppResponse is a shortcut for AppResponseBase<string>
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
        /// Get the list of AgentTemplate
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
                    List = listDto,
                    TotalCount = listDto.TotalCount,
                    PageIndex = listDto.PageIndex
                };
                return result;
            });
        }

        /// <summary>
        /// Delete files (delete database records and physical files at the same time)
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<bool>> DeleteFile(DeleteFileRequest request)
        {
            return await this.GetResponseAsync<bool>(async (response, logger) =>
            {
                await fileService.DeleteFileAsync(request.Id);
                return true;
            });
        }
    }
}
