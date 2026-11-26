/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiAppService.cs
    File description: ApiAppService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260729
    Change description: v0.17.1-preview6 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

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
    /// <summary>
    /// Exposes PromptRange diagnostic endpoints through the local API binding.
    /// </summary>
    public class ApiAppService : AppServiceBase
    {
        public ApiAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Executes the basic diagnostic endpoint and returns a fixed successful value.
        /// </summary>
        /// <returns>A response envelope containing the diagnostic value <c>200</c>.</returns>
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
        /// Exercises POST binding with a complex request and demonstrates the standard
        /// application-service exception and status-code pipeline.
        /// </summary>
        /// <param name="request">The request whose names are included in the diagnostic exception.</param>
        /// <returns>
        /// A string response envelope finalized with the custom error state after
        /// the intentional exception is handled.
        /// </returns>
        /// <remarks>
        /// This endpoint intentionally throws <see cref="NcfExceptionBase"/> for testing.
        /// </remarks>
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
