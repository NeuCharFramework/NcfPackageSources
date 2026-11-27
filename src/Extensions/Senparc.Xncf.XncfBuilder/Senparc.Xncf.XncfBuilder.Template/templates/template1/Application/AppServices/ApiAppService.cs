/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ApiAppService.cs
    File description: XNCF template sample implementation

    Created by: Senparc - 20211226

    Modified by: Senparc - 20260726
    Change description: v1.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261124
    Change description: v1.1.15 Integrated localization, tooling, and runtime improvements across the module.

    Modified by: Senparc - 20261127
    Change description: v1.1.16 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Template_OrgName.Xncf.Template_XncfName.Application.DTOs.Request;
using System;
using System.Threading.Tasks;


namespace Template_OrgName.Xncf.Template_XncfName.Application.AppServices
{
    public class ApiAppService : AppServiceBase
    {
        /// <summary>
        /// Initializes the sample API application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base application service.</param>
        public ApiAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /*
         * Legacy comment translated to English.

         * Legacy comment translated to English.

         * Legacy comment translated to English.

         */

        /// <summary>
        /// Demonstrates a minimal asynchronous application-service endpoint that returns a
        /// successful integer response after the sample operation completes.
        /// </summary>
        /// <returns>A response whose data value is the sample status value.</returns>
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
        /// Demonstrates a POST endpoint that receives a complex request, deliberately exercises
        /// application-service exception handling, and sets custom response state codes.
        /// </summary>
        /// <param name="request">The request containing the values included in the exception message.</param>
        /// <returns>A string response whose state reflects the handled exception.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is <see langword="null"/>.</exception>
        /// <exception cref="NcfExceptionBase">Thrown by the sample operation before the application-service exception handler handles it.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> MyCustomApi(Api_MyCustomApiRequest request)
        {
            //StringAppResponse is a shorthand for AppResponseBase<string>
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                throw new NcfExceptionBase(Template_XncfNameResource.Format(
                    "Frontend.Template_XncfName.ExceptionThrowTestParametersPassedValueValue",
                    "Exception test with parameters: {0} {1}",
                    request.FirstName,
                    request.LastName));
                response.StateCode = 100;
            },
            exceptionHandler: (ex, response, logger) =>
            {
                logger.Append(Template_XncfNameResource.Format(
                    "Function.Api.ExceptionHandled",
                    "Handled exception: {0}",
                    ex.Message));
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
