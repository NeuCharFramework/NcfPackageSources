/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Extensions.cs
    File description: Extensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    //Legacy comment translated to English.

    public static class Extensions
    {

        public static IApplicationBuilder UseSenparcMvcDI(this IApplicationBuilder builder)
        {
            DI.ServiceProvider = builder.ApplicationServices;
            return builder;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class DI
    {
        public static IServiceProvider ServiceProvider { get; set; }
    }
}
