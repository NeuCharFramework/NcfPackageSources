/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ServiceDataBase.cs
    File description: ServiceDataBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Repository;

namespace Senparc.Ncf.Service
{
    public interface IServiceDataBase
    {
        /// <summary>Gets or sets the provider used to resolve application services.</summary>
        IServiceProvider ServiceProvider { get; set; }
        /// <summary>Gets or sets the database abstraction used by the service.</summary>
        IDataBase BaseData { get; set; }
        /// <summary>Closes the current database connection through <see cref="BaseData"/>.</summary>
        void CloseConnection();

        /// <summary>Resolves an optional service from <see cref="ServiceProvider"/>.</summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The resolved service, or <see langword="null"/> when it is not registered.</returns>
        T GetService<T>();
    }


    /// <summary>
    /// Provides a service-level container for the database abstraction and dependency-injection provider.
    /// </summary>
    public class ServiceDataBase : IServiceDataBase
    {
        #region Legacy section

        /// <summary>
        /// Invoked after an object has been saved, with the database and saved object.
        /// </summary>
        public static Action<IDataBase, object>? AfterSaveObject { get; set; }

        /// <summary>
        /// Invoked after an object has been deleted, with the database and deleted object.
        /// </summary>
        public static Action<IDataBase, object>? AfterDeleteObject { get; set; }

        /// <summary>
        /// Invoked after the current unit of work has saved its pending changes.
        /// </summary>
        public static Action<IDataBase>? AfterSaveChanges { get; set; }

        #endregion


        public IServiceProvider ServiceProvider { get; set; }
        public IDataBase BaseData { get; set; }

        /// <summary>
        /// Initializes the service data container.
        /// </summary>
        /// <param name="baseData">The database abstraction used by the service.</param>
        /// <param name="serviceProvider">An optional service provider used for dependency resolution.</param>
        public ServiceDataBase(IDataBase baseData,IServiceProvider serviceProvider=null)
        {
            BaseData = baseData;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Closes the database connection exposed by <see cref="BaseData"/>.
        /// </summary>
        public virtual void CloseConnection()
        {
            BaseData.CloseConnection();
        }

        /// <summary>
        /// Resolves an optional service from the configured provider.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The resolved service, or <see langword="null"/> when no registration exists.</returns>
        public T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Resolves a required service from the configured provider.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the requested service is not registered.</exception>
        public T GetRequiredService<T>()
        {
            return ServiceProvider.GetRequiredService<T>();
        }

    }
}
