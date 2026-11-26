/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MySqlWithBackupDatabaseConfiguration.cs
    File description: MySqlWithBackupDatabaseConfiguration related implementation

    Created by: Senparc - 20201104

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;
using System.Data.Common;
using MySql.Data.MySqlClient;
using Senparc.Ncf.Database.MySql;

namespace Senparc.Ncf.Database.MySql.Backup
{
    /// <summary>
    /// MySQL database configuration retaining the legacy backup extension point.
    /// The current implementation does not generate backup SQL and returns no command.
    /// </summary>
    public class MySqlWithBackupDatabaseConfiguration : MySqlDatabaseConfiguration
    {
        /// <summary>
        /// Initializes the MySQL configuration that retains the backup extension point.
        /// </summary>
        public MySqlWithBackupDatabaseConfiguration() { }


        /// <inheritdoc />
        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            //Legacy comment translated to English.

            //string constring = Senparc.Ncf.Core.Config.SenparcDatabaseConnectionConfigs.ClientConnectionString;
            //using (var conn = new MySqlClient.MySqlConnection(constring))
            //{
            //    using (var cmd = new global:: MySql.Data.MySqlClient.MySqlCommand())
            //    {
            //        using (MySqlBackup mb = new MySqlBackup(cmd))
            //        {
            //            cmd.Connection = conn;
            //            conn.Open();
            //            mb.ExportToFile(backupFilePath);
            //            conn.Close();
            //        }
            //    }
            //}

            return null;
        }
    }
}
