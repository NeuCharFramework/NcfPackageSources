using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Utilities;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace Senparc.Web
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SenparcDbContextFactory : IDesignTimeDbContextFactory<SenparcEntities>
    {
        public SenparcEntities CreateDbContext(string[] args)
        {
            //Legacy comment translated to English.
            IRegisterService register = RegisterService.Start(new SenparcSetting());
            CO2NET.Config.RootDictionaryPath = Path.Combine(AppContext.BaseDirectory, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}"); //

            var builder = new DbContextOptionsBuilder<SenparcEntities>();

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            var sqlConnection = SenparcDatabaseConfigs.ClientConnectionString; //?? "Server=.\\;Database=NCF;Trusted_Connection=True;integrated security=True;";
            builder.UseSqlServer(sqlConnection, b => b.MigrationsAssembly("Senparc.Web"));
            return new SenparcEntities(builder.Options);
        }
    }
}
