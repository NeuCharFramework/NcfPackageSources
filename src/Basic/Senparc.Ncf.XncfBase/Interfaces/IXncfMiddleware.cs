/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IXncfMiddleware.cs

    File description: IXncfMiddleware related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IXncfMiddleware
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        IApplicationBuilder UseMiddleware(IApplicationBuilder app);
    }
}
