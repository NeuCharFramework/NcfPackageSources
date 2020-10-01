using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Database
{
    public class DatabaseConfigurationFactory
    {
        public static IDatabaseConfiguration DatabaseConfiguration { get; set; }

        public static IDatabaseConfiguration GetDatabaseConfiguration()
        {
            return DatabaseConfiguration;
        }

        public static IApplicationBuilder UseDatabase<TDatabaseConfiguration>(IApplicationBuilder app)
            where TDatabaseConfiguration : IDatabaseConfiguration, new()
            //where TDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<TDbContextOptionsBuilder, TOptionExtension>, new()
            //where TOptionExtension : RelationalOptionsExtension, new()
        {
            DatabaseConfiguration = new TDatabaseConfiguration();
            return app;
        }
    }
}
