using Senparc.CO2NET;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Template_OrgName.Xncf.Template_XncfName.OHS.Local.AppService
{
    public class ApiAppService : AppServiceBase
    {
        public ApiAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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
            return await this.GetResponseAsync<AppResponseBase<int>, int>(async (response, logger) =>
            {
                await Task.Delay(100);
                return 666;
            });
        }

        /// <summary>
        /// 将 AppService 暴露为 WebApi，自定义 API 名称
        /// </summary>
        /// <returns></returns>
        [ApiBind("MyCategory","MyApi")]
        public async Task<StringAppResponse> MyCustomApi()
        {
            //StringAppResponse 是 AppResponseBase<string> 的快捷写法
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                await Task.Delay(100);
                return "This is MyCustomApi";
            });
        }
    }
}
