/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20250113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.SenMapic
{
    public partial class Register : IXncfDatabase  //register the XNCF module database (optional)
    {
        #region Legacy section

        /// <summary>
        /// database prefix
        /// </summary>
        public const string DATABASE_PREFIX = "Senparc_SenMapic_";

        /// <summary>
        /// database prefix
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        /// <summary>
        /// retrieve the database context dynamically
        /// </summary>
        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        /// <summary>
        /// Applies SenMapic entity mappings to the module database model.
        /// The current module relies on convention-based and automatic mappings.
        /// </summary>
        /// <param name="modelBuilder">The Entity Framework Core model builder.</param>
        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //after implementing the [XncfAutoConfigurationMapping] attribute, this runs automatically and no manual registration is needed
            //modelBuilder.ApplyConfiguration(new AreaTemplate_ColorConfigurationMapping());
        }

        /// <summary>
        /// Registers SenMapic database services.
        /// The current module has no additional registrations beyond shared infrastructure.
        /// </summary>
        /// <param name="services">The application dependency-injection service collection.</param>
        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //DOT REMOVE OR MODIFY THIS LINE - Entities Point
            //ex. services.AddScoped(typeof(Color));
        }

        #endregion
    }
}
