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
    /// Oracle V11 数据库配置（包括 V11.2 等版本）
    /// <para>注意：如果使用 Oracle 12 或以上版本，请直接使用 OracleDatabaseConfiguration</para>
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
