/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IsolatedRuntimeServiceCollectionExtensions.cs
    文件功能描述：共享隔离运行时依赖注入注册

    创建标识：Senparc - 20261024
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
