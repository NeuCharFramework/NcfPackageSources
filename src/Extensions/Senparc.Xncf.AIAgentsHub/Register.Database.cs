using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIAgentsHub
{
    public partial class Register : IXncfDatabase  //Register the XNCF module database (optional)
    {
        #region IXncfDatabase interface/// <summary>
        /// Database prefix
        /// </summary>
        public const string DATABASE_PREFIX = "Senparc_AIAgentsHub_";

        /// <summary>
        /// Database prefix
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        /// <summary>
        /// Dynamically obtain database context
        /// </summary>
        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //accomplish[XncfAutoConfigurationMapping] After the feature is added, it can be executed automatically without adding it manually.
            //modelBuilder.ApplyConfiguration(new AreaTemplate_ColorConfigurationMapping());
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this bank - Entities Point
            //ex. services.AddScoped(typeof(Color));
        }

        #endregion
    }
}
