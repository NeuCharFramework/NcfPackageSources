/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：GenerateAppServiceInterface.cs
    文件功能描述：GenerateAppServiceInterface 相关实现
    
    
    创建标识：Senparc - 20220205
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.37.0-preview5 增强 XNCF 构建、数据库迁移与 AI 生成流程的本地化支持

----------------------------------------------------------------*/

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
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.GenerateInterface.Name", "Function.XncfBuilder.GenerateInterface.Description", typeof(Register))]
        public Task<StringAppResponse> Generate(GenerateAppServiceInterface_GenerateRequest request)
        {
            return this.GetStringResponseAsync((response, logger) => Task.FromResult(""));
        }
    }
}
