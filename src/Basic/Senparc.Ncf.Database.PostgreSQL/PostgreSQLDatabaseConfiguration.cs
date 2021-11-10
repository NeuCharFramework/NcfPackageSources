using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using Senparc.Ncf.Core.Models;
using System.Data.Common;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Senparc.Ncf.Database.Helpers;
using System.Diagnostics;

namespace Senparc.Ncf.Database.PostgreSQL
{
    /// <summary>
    /// PostgreSQL 数据库配置
    /// </summary>
    public class PostgreSQLDatabaseConfiguration : DatabaseConfigurationBase<NpgsqlDbContextOptionsBuilder, NpgsqlOptionsExtension>
    {
        public PostgreSQLDatabaseConfiguration() { }

        public override MultipleDatabaseType MultipleDatabaseType => MultipleDatabaseType.PostgreSQL;

        public override Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> DbContextOptionsActionExtension => (optionsBuilder, xncfDatabaseData) =>
        {
            var typedBuilder = optionsBuilder as NpgsqlDbContextOptionsBuilder;
            typedBuilder.EnableRetryOnFailure(
                       maxRetryCount: 5,
                       maxRetryDelay: TimeSpan.FromSeconds(5),
                       errorCodesToAdd: new string[] { "2" });
        };

        public override Action<DbContextOptionsBuilder, string, XncfDatabaseData, Action<IRelationalDbContextOptionsBuilderInfrastructure>> SetUseDatabase =>
            (optionsBuilder, connectionString, xncfDatabaseData, actionBase) =>
            {
                optionsBuilder.UseNpgsql(connectionString, actionBase);
            };

        public override string GetBackupDatabaseSql(DbConnection dbConnection, string backupFilePath)
        {
            //pg_dump.exe --file "F:\\psql.bak" --host "localhost" --port "5432" --username "postgres" --no-password --verbose --format=c --blobs "postgres"
            var dic = NcfDatabaseHelper.GetCurrentConnectionInfo();

            var host = NcfDatabaseHelper.TryGetConnectionValue(dic, "host");
            host = host != null ? $" --host \"{host}\"" : null;

            var port = NcfDatabaseHelper.TryGetConnectionValue(dic, "port");
            port = port != null ? $" --port \"{port}\"" : null;

            var username = NcfDatabaseHelper.TryGetConnectionValue(dic, "username");
            username = username != null ? $" --username \"{username}\"" : null;

            var cmd = $"pg_dump.exe --file \"{backupFilePath}\" {host}{port}{username}";

            var commandTexts = new List<string> {
                cmd,
            };
          
            Func<Process> getNewProcess = () =>
            {
                Process p = new Process();
                p.StartInfo.FileName = "pg_dump.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                return p;
            };

            Action<Process> closeProcess = p =>
            {
                p.WaitForExit();
                p.Close();
            };

            {
                Process cmdProcess = getNewProcess();
                Console.WriteLine(cmd);
                cmdProcess.StandardInput.WriteLine(cmd);
                cmdProcess.StandardInput.WriteLine("exit");//需要执行exit后才能读取 StandardOutput
                var output = cmdProcess.StandardOutput.ReadToEnd();
                Console.WriteLine("pg_dump.exe outupt: "+output);
                closeProcess(cmdProcess);
            }

            var password = NcfDatabaseHelper.TryGetConnectionValue(dic, "password");
            if (password != null)
            {
                Process passwordProcess = getNewProcess();
                Console.WriteLine(cmd);
                passwordProcess.StandardInput.WriteLine(password);
                passwordProcess.StandardInput.WriteLine("exit");//需要执行exit后才能读取 StandardOutput
                var output = passwordProcess.StandardOutput.ReadToEnd();
                Console.WriteLine("pg_dump.exe outupt: " + output);
                closeProcess(passwordProcess);
            }

            return "";
            //return $@"Backup Database {dbConnection.Database} To disk='{backupFilePath}'";
        }

        public override string GetDropTableSql(DbContext dbContext, string tableName)
        {
            //var schma = dbContext.Model.FindEntityType(type).GetSchema();
            //TODO: 增加 schma
            return $"DROP TABLE {tableName}";
        }


    }
}
