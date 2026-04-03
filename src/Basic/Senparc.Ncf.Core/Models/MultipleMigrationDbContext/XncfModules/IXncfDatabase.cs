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
    ///XNCF module database configuration
    /// </summary>
    public interface IXncfDatabase
    {
        /// <summary>
        /// Globally unique prefix for database tables, be sure to avoid duplication with other modules
        /// </summary>
        string DatabaseUniquePrefix { get; }
        /// <summary>
        ///Create database model
        /// </summary>
        void OnModelCreating(ModelBuilder modelBuilder);

        ///// <summary>
        ///// Set up the database, mainly provided for use
        ///// </summary>
        ///// <param name="dbContextOptionsAction"></param>
        ///// <param name="assemblyName">The assembly name of MigrationsAssembly. If it is null, the assembly where the current XncfDatabaseDbContextType is located will be used by default</param>
        //void DbContextOptionsAction(IRelationalDbContextOptionsBuilderInfrastructure dbContextOptionsAction,
        //                            string assemblyName = null);

        ///// <summary>
        ///// XncfDatabaseDbContext type
        ///// </summary>
        //Type XncfDatabaseDbContextType { get; }

        /// <summary>
        ///Add database module
        /// </summary>
        /// <param name="services"></param>
        void AddXncfDatabaseModule(IServiceCollection services);


        /// <summary>
        /// Try to obtain the database context type under the current configuration. If the corresponding database is not found, an exception will be thrown: <see cref="NcfDatabaseException"/>)
        /// </summary>
        /// <exception cref="NcfDatabaseException"></exception>
        /// <returns></returns>
        Type TryGetXncfDatabaseDbContextType { get; }
    }
}
