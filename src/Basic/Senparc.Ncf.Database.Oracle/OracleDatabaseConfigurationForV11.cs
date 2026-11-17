/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: OracleDatabaseConfigurationForV11.cs
    File description: OracleDatabaseConfigurationForV11 related implementation

    Created by: Senparc - 20220818

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
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
