/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiAppService.cs
    File description: ApiAppService related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Azure;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.DynamicData.OHS.Local.PL;
using System;
using System.Threading.Tasks;


namespace Senparc.Xncf.DynamicData.OHS.Local.AppService
{
    [ApiAuthorize]
    public class ApiAppService : AppServiceBase
    {
        /// <summary>
        /// Initializes the sample API application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public ApiAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /*

         * Legacy comment translated to English.

         * Legacy comment translated to English.

         * Legacy comment translated to English.

         */

        /// <summary>
        /// Returns the fixed sample status value through the standard application response pipeline.
        /// </summary>
        /// <returns>An application response whose payload is <c>200</c>.</returns>
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
        /// Exercises complex POST binding and the application's exception/status-code handling pipeline.
        /// </summary>
        /// <param name="request">The request values included in the diagnostic exception message.</param>
        /// <returns>A string application response; the configured response handler records the test exception.</returns>
        /// <exception cref="NcfExceptionBase">Intentionally thrown to exercise custom exception handling.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> MyCustomApi(Api_MyCustomApiRequest request)
        {
            //StringAppResponse is a shorthand for AppResponseBase<string>
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
    }
}
