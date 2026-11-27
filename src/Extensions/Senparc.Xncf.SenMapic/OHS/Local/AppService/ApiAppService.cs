/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiAppService.cs
    File description: ApiAppService related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AreaBase.Admin.Filters;
using Senparc.Xncf.SenMapic.OHS.Local.PL;
using System;
using System.Threading.Tasks;


namespace Senparc.Xncf.SenMapic.OHS.Local.AppService
{
    [ApiAuthorize]
    public class ApiAppService : AppServiceBase
    {
        /// <summary>
        /// Initializes the SenMapic sample API application service.
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
        /// Returns the fixed sample status value through the API response pipeline.
        /// </summary>
        /// <returns>An application response containing <c>200</c>.</returns>
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
        /// Exercises complex POST binding and custom exception/status handling.
        /// </summary>
        /// <param name="request">The request values included in the diagnostic exception.</param>
        /// <returns>A string application response produced by the response pipeline.</returns>
        /// <exception cref="NcfExceptionBase">Intentionally thrown to exercise exception handling.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> MyCustomApi(Api_MyCustomApiRequest request)
        {
            //StringAppResponse is a shorthand for AppResponseBase<string>
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                throw new NcfExceptionBase($"Exception test; parameters: {request.FirstName} {request.LastName}");
                response.StateCode = 100;
            },
            exceptionHandler: (ex, response, logger) =>
            {
                logger.Append($"Handling exception: {ex.Message}");
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
