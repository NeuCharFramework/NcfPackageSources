/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20200820

    Modified by: Senparc - 20261119
    Change description: v1.1.15 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;

namespace Template_OrgName.Xncf.Template_XncfName
{
    public partial class Register : IXncfDatabase  //register the XNCF module database (optional)
    {
        #region Legacy section

        /// <summary>
        /// database prefix
        /// </summary>
        public const string DATABASE_PREFIX = "Template_OrgName_Template_XncfName_";

        /// <summary>
        /// database prefix
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        /// <summary>
        /// retrieve the database context dynamically
        /// </summary>
        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        /// <summary>
        /// Applies module-specific Entity Framework Core model configuration.
        /// Convention and attribute-based mappings are sufficient for the template sample.
        /// </summary>
        /// <param name="modelBuilder">The model builder supplied by Entity Framework Core.</param>
        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //after implementing the [XncfAutoConfigurationMapping] attribute, this runs automatically and no manual registration is needed
            //modelBuilder.ApplyConfiguration(new AreaTemplate_ColorConfigurationMapping());
        }

        /// <summary>
        /// Registers template-specific database services.
        /// Add module entity registrations here when a generated module introduces
        /// services that are not covered by the standard XNCF registration pipeline.
        /// </summary>
        /// <param name="services">The application service collection.</param>
        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //DOT REMOVE OR MODIFY THIS LINE - Entities Point
            //ex. services.AddScoped(typeof(Color));
        }

        #endregion
    }
}
