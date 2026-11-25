/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Extensions.cs
    File description: Extensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Registers the application's service provider in the legacy NCF DI access point.
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Stores the application's service provider and returns the builder for fluent startup configuration.
        /// </summary>
        /// <param name="builder">The application builder whose service provider is stored.</param>
        /// <returns>The same application builder.</returns>
        public static IApplicationBuilder UseSenparcMvcDI(this IApplicationBuilder builder)
        {
            DI.ServiceProvider = builder.ApplicationServices;
            return builder;
        }
    }

    /// <summary>
    /// Provides the legacy process-wide service-provider access point used by NCF utility helpers.
    /// </summary>
    public static class DI
    {
        /// <summary>
        /// Gets or sets the service provider used by legacy static helpers.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; set; }
    }
}
