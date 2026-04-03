using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;
using System.Data.Common;
using Oracle.EntityFrameworkCore.Infrastructure;
using Oracle.EntityFrameworkCore.Infrastructure.Internal;
using Senparc.CO2NET.Extensions;

namespace Senparc.Ncf.Database.Oracle
{
    /// <summary>
    ///Oracle V11 database configuration (including V11.2 and other versions)
    /// <para>Note: If using Oracle 12 or above, please use OracleDatabaseConfiguration directly</para>
    /// </summary>
    public class OracleDatabaseConfigurationForV11 : OracleDatabaseConfiguration
    {
        public OracleDatabaseConfigurationForV11() : base()
        {
            OracleDatabaseConfiguration.SetUseOracleSQLCompatibility("11");
        }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Oracle;
    }
}
