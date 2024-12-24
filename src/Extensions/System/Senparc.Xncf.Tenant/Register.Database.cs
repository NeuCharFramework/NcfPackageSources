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
        public const string DATABASE_PREFIX = NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//系统表，

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
