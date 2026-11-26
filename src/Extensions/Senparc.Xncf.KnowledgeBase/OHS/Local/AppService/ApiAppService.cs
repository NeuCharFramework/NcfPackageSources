/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiAppService.cs
    File description: ApiAppService related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.KnowledgeBase.OHS.Local.PL;
using System;
using System.Threading.Tasks;


namespace Senparc.Xncf.KnowledgeBase.OHS.Local.AppService
{
    /// <summary>
    /// Exposes small diagnostic KnowledgeBase endpoints through the local API binding.
    /// </summary>
    public class ApiAppService : AppServiceBase
    {
        public ApiAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Executes the basic diagnostic endpoint and returns a successful integer payload.
        /// </summary>
        /// <returns>
        /// A response envelope containing the fixed diagnostic value
        /// <c>200</c>, unless the response pipeline itself fails.
        /// </returns>
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
        /// Exercises POST binding with a complex request and demonstrates custom
        /// exception handling and response status assignment.
        /// </summary>
        /// <param name="request">The request containing the values included in the diagnostic exception message.</param>
        /// <returns>
        /// A string response envelope whose error state is adjusted by the configured
        /// exception and finalization callbacks.
        /// </returns>
        /// <remarks>
        /// This endpoint intentionally raises an <see cref="NcfExceptionBase"/> to
        /// demonstrate the standard application-service error path.
        /// </remarks>
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
