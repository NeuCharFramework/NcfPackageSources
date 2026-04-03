using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.SenMapic
{
    public partial class Register : IXncfDatabase  //Register the XNCF module database (optional)
    {
        #region IXncfDatabase 接口

        /// <summary>
        ///database prefix
        /// </summary>
        public const string DATABASE_PREFIX = "Senparc_SenMapic_";

        /// <summary>
        ///database prefix
        /// </summary>
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        /// <summary>
        /// Dynamically obtain database context
        /// </summary>
        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            //After implementing the [XncfAutoConfigurationMapping] feature, it can be executed automatically without adding it manually.
            //modelBuilder.ApplyConfiguration(new AreaTemplate_ColorConfigurationMapping());
        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //DOT REMOVE OR MODIFY THIS LINE Do not remove or modify this LINE - Entities Point
            //ex. services.AddScoped(typeof(Color));
        }

        #endregion
    }
}
