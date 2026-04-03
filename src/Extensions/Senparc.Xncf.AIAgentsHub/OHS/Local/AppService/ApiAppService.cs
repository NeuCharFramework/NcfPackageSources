using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIAgentsHub.OHS.Local.PL;
using System;
using System.Threading.Tasks;


namespace Senparc.Xncf.AIAgentsHub.OHS.Local.AppService
{
    public class ApiAppService : AppServiceBase
    {
        public ApiAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /*
         * use[ApiBind] Any method or class can be quickly created into a dynamic WebApi.* In DDD systems, for security and anti-corrosion considerations, it is recommended to only use AppService.* When added on AppService[ApiBind] When the label cannot meet the needs, you can still create an ApiController manually.*/

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
        /// Customize Post types and complex parameters, while testing exception throwing and custom status codes
        /// </summary>
        /// <returns></returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> MyCustomApi(Api_MyCustomApiRequest request)
        {
            //StringAppResponse is AppResponseBase<string> shortcut for writing
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                throw new NcfExceptionBase($"Throw an exception test and transfer parameters:{request.FirstName} {request.LastName}");
                response.StateCode = 100;
            },
            exceptionHandler: (ex, response, logger) =>
            {
                logger.Append($"Handling exception, message:{ex.Message}");
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
