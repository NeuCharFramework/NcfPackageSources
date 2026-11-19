
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Xncf.MCP.Models.DatabaseModel;
using System;
using System.IO;

namespace Senparc.Xncf.MCP.Models
{
    [MultipleMigrationDbContext(MultipleDatabaseType.SqlServer, typeof(Register))]
    public class MCPSenparcEntities_SqlServer : MCPSenparcEntities
    {
        public MCPSenparcEntities_SqlServer(DbContextOptions<MCPSenparcEntities_SqlServer> dbContextOptions) : base(dbContextOptions)
        {
        }
    }


    /// <summary>
    /// design-time DbContext creation (used only to create Code-First database migrations during development; not executed in production)
    /// <para>1、switch to Debug mode</para>
    /// <para>2、run：PM> add-migration [updatename] -c MCPSenparcEntities_SqlServer -o Domain/Migrations/Migrations.SqlServer </para>
    /// </summary>
    public class SenparcDbContextFactory_SqlServer : SenparcDesignTimeDbContextFactoryBase<MCPSenparcEntities_SqlServer, Register>
    {
        protected override Action<IApplicationBuilder> AppAction => app =>
        {
            //specify another database
            app.UseNcfDatabase("Senparc.Ncf.Database.SqlServer", "Senparc.Ncf.Database.SqlServer", "SqlServerDatabaseConfiguration");
        };

        public SenparcDbContextFactory_SqlServer() : base(SenparcDbContextFactoryConfig.RootDirectoryPath)
        {

        }
    }
}
