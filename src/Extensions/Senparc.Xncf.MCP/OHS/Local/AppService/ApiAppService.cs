/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiAppService.cs
    File description: ApiAppService related implementation

    Created by: Senparc - 20250325

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.5.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.5.14.2 Expand MCP endpoint gateway, credentials, and tool invocation support.

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.MCP.OHS.Local.PL;
using System;
using System.Threading.Tasks;


namespace Senparc.Xncf.MCP.OHS.Local.AppService
{
    public class ApiAppService : AppServiceBase
    {
        /// <summary>
        /// Initializes the sample MCP API application service.
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
        /// Demonstrates a minimal asynchronous API endpoint that returns the sample status value.
        /// </summary>
        /// <returns>A response containing the sample integer result.</returns>
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
        /// Demonstrates complex POST binding and application-service exception handling.
        /// </summary>
        /// <param name="request">The values included in the sample exception message.</param>
        /// <returns>A string response whose state reflects the handled exception.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
        /// <exception cref="NcfExceptionBase">Thrown by the sample operation before the configured handler handles it.</exception>
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
