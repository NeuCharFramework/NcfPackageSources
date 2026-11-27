/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: OracleDatabaseConfigurationForV11.cs
    File description: OracleDatabaseConfigurationForV11 related implementation

    Created by: Senparc - 20220818

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.10.14 Updated database integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.10.14.2 Improve source documentation, headers, and module metadata consistency.

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
    /// Oracle database configuration preset that preserves the legacy Oracle 11
    /// compatibility entry point for callers that still use this type.
    /// The current net10 provider callback is inherited from
    /// <see cref="OracleDatabaseConfiguration"/>.
    /// </summary>
    public class OracleDatabaseConfigurationForV11 : OracleDatabaseConfiguration
    {
        /// <summary>
        /// Initializes the Oracle provider and records the legacy compatibility value <c>11</c>.
        /// </summary>
        public OracleDatabaseConfigurationForV11() : base()
        {
            OracleDatabaseConfiguration.SetUseOracleSQLCompatibility("11");
        }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.Oracle;
    }
}
