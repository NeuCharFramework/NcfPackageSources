/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IXncfDatabase.cs
    File description: IXncfDatabase related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Defines the database integration contract implemented by an XNCF module.
    /// </summary>
    public interface IXncfDatabase
    {
        /// <summary>
        /// Gets the table-name prefix used by the module's database objects.
        /// </summary>
        string DatabaseUniquePrefix { get; }
        /// <summary>
        /// Applies the module's Entity Framework Core entity and relationship mappings.
        /// </summary>
        void OnModelCreating(ModelBuilder modelBuilder);

        // The former direct DbContext-options callback members were removed from
        // the public contract; provider configuration now flows through AddXncfModule.

        /// <summary>
        /// Registers additional module database services.
        /// </summary>
        /// <param name="services">The application dependency-injection service collection.</param>
        void AddXncfDatabaseModule(IServiceCollection services);


        /// <summary>
        /// Resolves the provider-specific DbContext type used by this module.
        /// </summary>
        /// <exception cref="NcfDatabaseException">Thrown when no compatible module DbContext is registered.</exception>
        /// <returns>The provider-specific module DbContext type.</returns>
        Type TryGetXncfDatabaseDbContextType { get; }
    }
}
