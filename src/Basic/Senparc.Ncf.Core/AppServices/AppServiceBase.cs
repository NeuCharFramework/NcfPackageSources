/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AppServiceBase.cs
    File description: AppServiceBase related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

#nullable enable
namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// Defines the dependency-injection and cancellation context exposed by an application service.
    /// </summary>
    public interface IAppService
    {
        /// <summary>
        /// Gets the service provider used to resolve application-service dependencies.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets or sets the cancellation token used by the current application-service operation.
        /// </summary>
        CancellationToken CancellationToken { get; set; }
    }

    public abstract class AppServiceBase : IAppService
    {
        /// <summary>
        /// Gets the dependency-injection provider captured for this service instance.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets or sets the cancellation token that application-service methods should observe.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Initializes an application service with its dependency-injection provider.
        /// </summary>
        /// <param name="serviceProvider">The provider used to resolve service dependencies.</param>
        public AppServiceBase(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CancellationToken = new CancellationToken();
        }

        /// <summary>
        /// Resolves an optional service from the captured dependency-injection provider.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The registered service instance, or <see langword="null"/> when no registration exists.</returns>
        public T? GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Resolves a required service from the captured dependency-injection provider.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The registered service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service type is not registered.</exception>
        public T GetRequiredService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Resolves a required keyed service from the captured dependency-injection provider.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="serviceKey">The key used to distinguish the requested registration.</param>
        /// <returns>The registered keyed service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no matching keyed registration exists.</exception>
        public T GetRequiredKeyedService<T>(object? serviceKey) where T : notnull
        {
            return ServiceProvider.GetRequiredKeyedService<T>(serviceKey);
        }
    }
}
