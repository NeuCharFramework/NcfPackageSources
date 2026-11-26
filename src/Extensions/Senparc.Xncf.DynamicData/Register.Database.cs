/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20240718

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.7.12 Localized DynamicData configuration and layout surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Mapping;

namespace Senparc.Xncf.DynamicData
{
    public partial class Register : IXncfDatabase  //register the XNCF module database (optional)
    {
        #region Legacy section

        /// <summary>
        /// database prefix
        /// </summary>
        public const string DATABASE_PREFIX = "Senparc_DynamicData_";

        /// <summary>
        /// database prefix
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        /// <summary>
        /// retrieve the database context dynamically
        /// </summary>
        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        /// <summary>
        /// Applies DynamicData entity mappings to the module database model.
        /// The current module relies on convention and automatic mapping discovery,
        /// so no explicit mappings are registered here.
        /// </summary>
        /// <param name="modelBuilder">The EF Core model builder being configured.</param>
        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //after implementing the [XncfAutoConfigurationMapping] attribute, this runs automatically and no manual registration is needed
            //modelBuilder.ApplyConfiguration(new AreaTemplate_ColorConfigurationMapping());

            //modelBuilder.ApplyConfiguration(new DynamicData_ColumnMetadataConfigurationMapping());
            //modelBuilder.ApplyConfiguration(new DynamicData_TableDataConfigurationMapping());
        }

        /// <summary>
        /// Registers DynamicData-specific database services.
        /// The method is intentionally empty while the module has no additional
        /// service registrations beyond the shared database infrastructure.
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
