/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiAppService.cs
    File description: ApiAppService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260729
    Change description: v0.17.1-preview6 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Threading.Tasks;


namespace Senparc.Xncf.PromptRange.OHS.Local.AppService
{
    [ApiAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class ApiAppService : AppServiceBase
    {
        public ApiAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /*
         * Legacy comment translated to English.

         * Legacy comment translated to English.

         * Legacy comment translated to English.

         */

        /// <summary>
        /// expose AppService as a Web API
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
        /// custom Post types and complex parameters while testing exception throwing and custom status codes
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> MyCustomApi(Api_MyCustomApiRequest request)
        {
            //StringAppResponse is a shorthand for AppResponseBase<string>
            return await this.GetStringResponseAsync(async (response, logger) =>
                {
                    response.StateCode = 100;
                    throw new NcfExceptionBase($"抛出异常测试，传输参数：{request.FirstName} {request.LastName}");
                },
                exceptionHandler: (ex, response, logger) => { logger.Append($"正在处理异常，信息：{ex.Message}"); },
                afterFunc: (response, logger) =>
                {
                    if (response.Success != true)
                    {
                        response.StateCode = 101;
                    }
                });
        }
    }
}
