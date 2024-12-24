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
    /// MySQL（附带备份） 数据库配置，处于等待官方更新中，目前无效
    /// </summary>
    public class MySqlWithBackupDatabaseConfiguration : MySqlDatabaseConfiguration
    {
        public MySqlWithBackupDatabaseConfiguration() { }


        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            //需要等 Pomelo.EntityFrameworkCore.MySql 5.0才能支持：https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1226

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
