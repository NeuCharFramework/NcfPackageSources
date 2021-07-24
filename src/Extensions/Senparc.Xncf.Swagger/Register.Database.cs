using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Swagger
{
    public partial class Register : IXncfDatabase
    {
        public const string DATABASE_PREFIX = "Swagger";

        public string DatabaseUniquePrefix => DATABASE_PREFIX;
        public Type TryGetXncfDatabaseDbContextType => typeof(SwaggerEntities);

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            throw new NotImplementedException();
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            throw new NotImplementedException();
        }
    }
}
