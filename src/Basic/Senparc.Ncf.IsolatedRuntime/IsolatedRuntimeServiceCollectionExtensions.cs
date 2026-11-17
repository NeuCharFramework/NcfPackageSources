/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IsolatedRuntimeServiceCollectionExtensions.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.1.0-preview1 implement connection manager and workspace file system provider

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Ncf.IsolatedRuntime;

public static class IsolatedRuntimeServiceCollectionExtensions
{
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
