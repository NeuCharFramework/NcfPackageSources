/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IAreaRegister.cs

    File description: IAreaRegister related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Areas
{
    /// <summary>
    /// Register interface of Area
    /// </summary>
    public interface IAreaRegister
    {
        /// <summary>
        /// Authorization configuration
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        IMvcBuilder AuthorizeConfig(IMvcBuilder builder, Microsoft.Extensions.Hosting.IHostEnvironment/*IWebHostEnvironment*/ env);

        /// <summary>
        /// If a UI interface is provided, a home page must be specified, such as: Admin/
        /// <para>Note: This option only works with XncfRegister use. Otherwise, please ignore</para> it.
        /// </summary>
        string HomeUrl { get; }

        /// <summary>
        ///MenuItems
        /// </summary>
        List<AreaPageMenuItem> AreaPageMenuItems { get; }
    }
}
