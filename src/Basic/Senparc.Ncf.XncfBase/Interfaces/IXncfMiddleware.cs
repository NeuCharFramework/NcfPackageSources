/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IXncfMiddleware.cs
    File description: IXncfMiddleware related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Defines the optional middleware registration contract for an XNCF module.
    /// </summary>
    public interface IXncfMiddleware
    {
        /// <summary>
        /// Adds module middleware to the ASP.NET Core application pipeline.
        /// </summary>
        /// <param name="app">The application builder being configured.</param>
        /// <returns>The same builder after module middleware has been registered.</returns>
        IApplicationBuilder UseMiddleware(IApplicationBuilder app);
    }
}
