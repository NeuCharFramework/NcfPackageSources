using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{

    public class GenerateAppServiceInterface : AppServiceBase
    {
        public GenerateAppServiceInterface(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// 生成 生成 AppService 接口代码
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [FunctionRender("生成 AppService 接口代码", "将某个模块的 AppService ", typeof(Register))]
        public async Task<StringAppResponse> Generate(GenerateAppServiceInterface_GenerateRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                return "";
            });
        }
    }
}
