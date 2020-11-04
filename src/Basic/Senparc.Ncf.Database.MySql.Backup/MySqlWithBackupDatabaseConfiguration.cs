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
using MySqlClient = global:: MySql.Data.MySqlClient;

namespace Senparc.Ncf.Database.MySql.Backup
{
    public class MySqlWithBackupDatabaseConfiguration : MySqlDatabaseConfiguration
    {
        public MySqlWithBackupDatabaseConfiguration() { }


        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            string constring = Senparc.Ncf.Core.Config.SenparcDatabaseConnectionConfigs.ClientConnectionString;
            using (var conn = new MySqlClient.MySqlConnection(constring))
            {
                using (var cmd = new global:: MySql.Data.MySqlClient.MySqlCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        cmd.Connection = conn;
                        conn.Open();
                        mb.ExportToFile(backupFilePath);
                        conn.Close();
                    }
                }
            }

            return null;
        }
    }
}
