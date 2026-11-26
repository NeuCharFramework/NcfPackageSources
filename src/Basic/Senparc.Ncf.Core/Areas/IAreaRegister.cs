/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IAreaRegister.cs
    File description: IAreaRegister related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Areas
{
    /// <summary>
    /// Defines the registration contract for an XNCF administration area.
    /// </summary>
    public interface IAreaRegister
    {
        /// <summary>
        /// Applies authorization and Razor Pages configuration for the area.
        /// </summary>
        /// <param name="builder">The MVC builder being configured.</param>
        /// <param name="env">The hosting environment.</param>
        /// <returns>The configured MVC builder.</returns>
        IMvcBuilder AuthorizeConfig(IMvcBuilder builder, Microsoft.Extensions.Hosting.IHostEnvironment/*IWebHostEnvironment*/ env);

        /// <summary>
        /// If a UI interface is provided, a home page must be specified, such as: Admin/
        /// <para>Note: This option only works with XncfRegister use. Otherwise, please ignore</para> it.
        /// </summary>
        string HomeUrl { get; }

        /// <summary>
        /// Gets the navigation items exposed by the area.
        /// </summary>
        List<AreaPageMenuItem> AreaPageMenuItems { get; }
    }
}
