/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IXncfDatabase.cs

    File description: IXncfDatabase related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
    ///Legacy comment translated to English.
    /// </summary>
    public interface IXncfDatabase
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string DatabaseUniquePrefix { get; }
        /// <summary>
        /// createdatabasemodel
        /// </summary>
        void OnModelCreating(ModelBuilder modelBuilder);

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <param name="dbContextOptionsAction"></param>
        ///Legacy comment translated to English.
        //void DbContextOptionsAction(IRelationalDbContextOptionsBuilderInfrastructure dbContextOptionsAction,
        //                            string assemblyName = null);

        ///// <summary>
        ///// XncfDatabaseDbContext type
        ///// </summary>
        //Type XncfDatabaseDbContextType { get; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="services"></param>
        void AddXncfDatabaseModule(IServiceCollection services);


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <exception cref="NcfDatabaseException"></exception>
        /// <returns></returns>
        Type TryGetXncfDatabaseDbContextType { get; }
    }
}
