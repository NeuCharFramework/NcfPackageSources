using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AspNet.Areas
{
    /// <summary>
    /// 具有 Host 能力的（IWebHostEnvironment） Area 的 Register 接口
    /// </summary>
    public interface IAreaHostRegister
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        IMvcBuilder AuthorizeConfig(IMvcBuilder builder, Microsoft.Extensions.Hosting.IHostEnvironment/*IWebHostEnvironment*/ env);
    }
}
