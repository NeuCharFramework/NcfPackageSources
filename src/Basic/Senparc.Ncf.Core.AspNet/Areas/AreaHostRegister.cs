using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Areas;

namespace Senparc.Ncf.Core.AspNet.Areas
{
    /// <summary>
    /// 对所有扩展 Area（带 Host） 进行注册
    /// </summary>
    public static class AreaHostRegister
    {
        /// <summary>
        /// 自动注册所有 Area
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IMvcBuilder AddNcfAreasWithHost(this IMvcBuilder builder, IWebHostEnvironment env)
        {
            AreaRegister.AddNcfAreas(builder, register =>
            {
                if (register is IAreaHostRegister)
                {
                    (register as IAreaHostRegister).AuthorizeConfig(builder, env);//进行包含 Host 的注册过程
                }
            });

            return builder;
        }
    }
}