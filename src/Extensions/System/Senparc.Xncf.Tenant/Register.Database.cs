/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20211211

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Xncf.Tenant.Domain.DatabaseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//system tables，

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        /// <summary>
        /// Registers tenant database services that are not supplied by the shared database pipeline.
        /// </summary>
        /// <param name="services">The application service collection.</param>
        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            
        }

        /// <summary>
        /// Applies tenant Entity Framework Core model configuration.
        /// </summary>
        /// <param name="modelBuilder">The Entity Framework Core model builder.</param>
        public void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

    }
}
