using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Areas
{
    /// <summary>
    ///Area Register interface
    /// </summary>
    public interface IAreaRegister
    {
        /// <summary>
        ///Authorization configuration
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        IMvcBuilder AuthorizeConfig(IMvcBuilder builder, Microsoft.Extensions.Hosting.IHostEnvironment/*IWebHostEnvironment*/ env);

        /// <summary>
        /// If a UI interface is provided, a homepage must be specified, such as: Admin/
        /// <para>Note: This option needs to be used with XncfRegister to be effective, otherwise please ignore</para>
        /// </summary>
        string HomeUrl { get; }

        /// <summary>
        ///menu item
        /// </summary>
        List<AreaPageMenuItem> AreaPageMenuItems { get; }
    }
}
