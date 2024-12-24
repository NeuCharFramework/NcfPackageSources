using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.SystemPermission
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "SYSTEM_PERMISSION_";//NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//系统表，

        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {

        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}
