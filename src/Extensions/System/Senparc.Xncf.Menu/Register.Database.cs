/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.14.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Menu
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "SYSTEM_MENU_";

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        /// <summary>
        /// Registers Menu database services used by menu persistence.
        /// </summary>
        /// <param name="services">The application service collection.</param>
        public void AddXncfDatabaseModule(IServiceCollection services)
        {

        }

        /// <summary>
        /// Applies Menu Entity Framework Core model configuration.
        /// </summary>
        /// <param name="modelBuilder">The Entity Framework Core model builder.</param>
        public void OnModelCreating(ModelBuilder modelBuilder)
        {
           
        }
    }
}
