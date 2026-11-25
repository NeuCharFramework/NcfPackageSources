/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IsolatedRuntimeServiceCollectionExtensions.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.1.0-preview1 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.1.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Ncf.IsolatedRuntime;

public static class IsolatedRuntimeServiceCollectionExtensions
{
    /// <summary>
    /// Registers Docker-backed isolated-runtime services and binds their configuration section.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configuration">The application configuration containing the Docker runtime section.</param>
    /// <returns>The same service collection for fluent registration.</returns>
    public static IServiceCollection AddDockerIsolatedRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DockerIsolatedRuntimeOptions>(
            configuration.GetSection(DockerIsolatedRuntimeOptions.SectionName));
        services.TryAddSingleton<IIsolatedRuntimeProvider, DockerIsolatedRuntimeProvider>();
        return services;
    }
}
