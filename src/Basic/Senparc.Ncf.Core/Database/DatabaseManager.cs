using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Database
{
    public class DatabaseManager
        //<TDatabaseConfiguration, TDbContextOptionsBuilder, TOptionExtension>
        //where TDatabaseConfiguration : IDatabaseConfiguration<TDbContextOptionsBuilder, TOptionExtension>, new()
        //where TDbContextOptionsBuilder : RelationalDbContextOptionsBuilder<TDbContextOptionsBuilder, TOptionExtension>, new()
        //    where TOptionExtension : RelationalOptionsExtension, new()
    {
        public static IDatabaseConfiguration DatabaseConfiguration { get; set; }


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
