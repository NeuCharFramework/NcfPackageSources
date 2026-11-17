/*-----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: MultipleDatabasePool.cs

    File description: MultipleDatabasePool related implementation

    

    

    Created by: Senparc - 20201010

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260817

    Change description: v0.21.8-preview8 Updated module behavior and related support.



    Modified by: Senparc - 20260822

    Change description: v0.21.9 Updated module behavior and related support.



----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.Database
{
    public class XncfDatabaseDbContextWapper
    {
        /// <summary>
        /// 
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public XncfDatabaseDbContextWapper(Type migrationDbContextType, Type runtimeDbContextType)
        {
            MigrationDbContextType = migrationDbContextType;
            RuntimeDbContextType = runtimeDbContextType;
        }

        public Type MigrationDbContextType { get; set; }
        public Type RuntimeDbContextType { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// <para>Value for Dictionary<Type/* IXncfDatabase Register type*/, Type/* database XncfDatabaseDbContext type */></para>
    /// </summary>
    public class MultipleDatabasePool
        : Dictionary<MultipleDatabaseType, Dictionary<Type/* IXncfDatabase Register type*/, Type/* database XncfDatabaseDbContext type */>>
    {
        #region Legacy section

        MultipleDatabasePool() { }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static MultipleDatabasePool Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        class Nested
        {
            static Nested() { }

            internal static readonly MultipleDatabasePool instance = new MultipleDatabasePool();
        }

        #endregion

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static Type UnitTestPillarDbContext { get; set; } = null;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="multiDbContextAttr"></param>
        ///Legacy comment translated to English.
        public void TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType)
        {
            //Legacy comment translated to English.
            if (!this.ContainsKey(multiDbContextAttr.MultipleDatabaseType))
            {
                //Legacy comment translated to English.
                this[multiDbContextAttr.MultipleDatabaseType] = new Dictionary<Type, Type>();
            }

            //Legacy comment translated to English.
            this[multiDbContextAttr.MultipleDatabaseType][multiDbContextAttr.XncfDatabaseRegisterType] = xncfDatabaseDbContextType;

            //Legacy comment translated to English.
            XncfDatabaseDbContextPool.Instance.TryAdd(multiDbContextAttr, xncfDatabaseDbContextType);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public string BuildSupportMatrixLog(MultipleDatabaseType? currentDatabaseType = null)
        {
            return BuildSupportMatrixLog(XncfDatabaseDbContextPool.Instance, currentDatabaseType);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string BuildSupportMatrixLog(
            IDictionary<Type, Dictionary<MultipleDatabaseType, Type>> registerDbMap,
            MultipleDatabaseType? currentDatabaseType = null)
        {
            var sb = new StringBuilder();

            if (registerDbMap == null || registerDbMap.Count == 0)
            {
                sb.AppendLine(" === Multiple databases support matrix ===");
                sb.AppendLine(" (no MultipleMigrationDbContext found)");
                return sb.ToString().TrimEnd();
            }

            //Legacy comment translated to English.
            var dbColumns = registerDbMap.Values
                .SelectMany(z => z.Keys)
                .Concat(currentDatabaseType.HasValue ? new[] { currentDatabaseType.Value } : Array.Empty<MultipleDatabaseType>())
                .Where(z => z != MultipleDatabaseType.Other)
                .Distinct()
                .OrderBy(z => (int)z)
                .ToList();

            var registers = registerDbMap.Keys
                .OrderBy(z => z.FullName, StringComparer.Ordinal)
                .ToList();

            const string registerHeader = "Register";
            var registerWidth = Math.Max(registerHeader.Length, registers.Max(z => (z.FullName ?? z.Name).Length));
            var colWidths = dbColumns.ToDictionary(
                db => db,
                db => Math.Max(db.ToString().Length, 3));

            sb.AppendLine(" === Multiple databases support matrix ===");
            sb.AppendLine(" Legend: [Y]=supported  [-]=unsupported  [*]=IN USE (current)");
            sb.AppendLine(currentDatabaseType.HasValue
                ? $" Current Database: {currentDatabaseType.Value}"
                : " Current Database: (unknown at scan time)");

            // header
            sb.Append("| ").Append(registerHeader.PadRight(registerWidth));
            foreach (var db in dbColumns)
            {
                sb.Append(" | ").Append(db.ToString().PadRight(colWidths[db]));
            }
            sb.AppendLine(" |");

            // separator
            sb.Append("| ").Append(new string('-', registerWidth));
            foreach (var db in dbColumns)
            {
                sb.Append(" | ").Append(new string('-', colWidths[db]));
            }
            sb.AppendLine(" |");

            // rows
            foreach (var registerType in registers)
            {
                var registerName = registerType.FullName ?? registerType.Name;
                var dbMap = registerDbMap[registerType];
                sb.Append("| ").Append(registerName.PadRight(registerWidth));
                foreach (var db in dbColumns)
                {
                    string cell;
                    if (currentDatabaseType.HasValue && db == currentDatabaseType.Value && dbMap.ContainsKey(db))
                    {
                        cell = "*";
                    }
                    else if (dbMap.ContainsKey(db))
                    {
                        cell = "Y";
                    }
                    else
                    {
                        cell = "-";
                    }
                    sb.Append(" | ").Append(cell.PadRight(colWidths[db]));
                }
                sb.AppendLine(" |");
            }

            //Legacy comment translated to English.
            sb.AppendLine();
            if (currentDatabaseType.HasValue)
            {
                var currentDb = currentDatabaseType.Value;
                sb.AppendLine($" === Current DbContext bindings ({currentDb}) ===");
                const string dbContextHeader = "DbContext Type";
                var bindingRows = registers
                    .Select(r =>
                    {
                        registerDbMap[r].TryGetValue(currentDb, out var dbContextType);
                        return (Register: r.FullName ?? r.Name, DbContext: dbContextType?.Name);
                    })
                    .ToList();

                var dbContextWidth = Math.Max(
                    dbContextHeader.Length,
                    bindingRows.Max(z => (z.DbContext ?? "(not supported)").Length));

                sb.Append("| ").Append(registerHeader.PadRight(registerWidth))
                    .Append(" | ").Append(dbContextHeader.PadRight(dbContextWidth)).AppendLine(" |");
                sb.Append("| ").Append(new string('-', registerWidth))
                    .Append(" | ").Append(new string('-', dbContextWidth)).AppendLine(" |");

                foreach (var row in bindingRows)
                {
                    var dbContextName = row.DbContext ?? "(not supported)";
                    sb.Append("| ").Append(row.Register.PadRight(registerWidth))
                        .Append(" | ").Append(dbContextName.PadRight(dbContextWidth)).AppendLine(" |");
                }
            }
            else
            {
                sb.AppendLine(" === Current DbContext bindings ===");
                sb.AppendLine(" (skipped: current database type not available during StartNcfEngine scan)");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public Type GetXncfDbContextType(IXncfDatabase xncfDatabaseRegister)
        {
            return GetXncfDbContextType(xncfDatabaseRegister.GetType());
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public Type GetXncfDbContextType(Type xncfDatabaseRegisterType)
        {
            //Legacy comment translated to English.
            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            //currentdatabaseconfiguration
            var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
            //currentdatabasetype
            MultipleDatabaseType multipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;

            if (multipleDatabaseType == MultipleDatabaseType.InMemory)
            {
                //Legacy comment translated to English.
                return UnitTestPillarDbContext ?? throw new NcfExceptionBase($"当前数据库类型为 {multipleDatabaseType}，需要指定 {nameof(UnitTestPillarDbContext)}！");
            }
            else if (!this.ContainsKey(multipleDatabaseType))
            {
                throw new NcfDatabaseException($"未发现任何支持此数据库类型的 XNCF 模块：{multipleDatabaseType}", currentDatabaseConfiguration.GetType());
            }

            var xncdDatabaseRegisterCollection = this[multipleDatabaseType];
            if (!xncdDatabaseRegisterCollection.ContainsKey(xncfDatabaseRegisterType))
            {
                throw new NcfDatabaseException($"{xncfDatabaseRegisterType.FullName} 模块未支持数据库：{multipleDatabaseType}", currentDatabaseConfiguration.GetType());
            }

            return xncdDatabaseRegisterCollection[xncfDatabaseRegisterType];
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <param name="serviceProvider">ServiceProvider</param>
        /// <returns></returns>
        public T GetDbContext<T>(string connectionString = null, XncfDatabaseData xncfDatabaseData = null,
            Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null, IServiceProvider serviceProvider = null) where T : DbContext
        {
            var dbContextType = typeof(T);
            DbContextOptionsBuilder dbOptionBuilder;

            var dbOptionBuilderType = dbContextType.GetConstructors().First()
                                        .GetParameters().First().ParameterType;

            if (dbOptionBuilderType.GenericTypeArguments.Length > 0)
            {
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                dbOptionBuilderType = typeof(DbContextOptionsBuilder<>);
                //dbOptionBuilderType = typeof(RelationalDbContextOptionsBuilder<,>);
                //Legacy comment translated to English.
                dbOptionBuilderType = dbOptionBuilderType.MakeGenericType(dbContextType);

                //Legacy comment translated to English.
                dbOptionBuilder = Activator.CreateInstance(dbOptionBuilderType) as DbContextOptionsBuilder;
            }
            else
            {
                //Legacy comment translated to English.
                dbOptionBuilder = new DbContextOptionsBuilder();
            }

            //if (UnitTestDatabaseConfiguration.UnitTestPillarDbContext == null)
            {
                //Legacy comment translated to English.

                //getcurrentdatabaseconfiguration
                var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.Current;
                //Legacy comment translated to English.
                currentDatabasConfiguration.UseDatabase(
                    dbOptionBuilder,
                    connectionString ?? (SenparcDatabaseConnectionConfigs.GetClientConnectionString()),
                    xncfDatabaseData,
                    dbContextOptionsAction
                    );
            }

            //Legacy comment translated to English.
            T dbContext;
            if (serviceProvider == null)
            {
                dbContext = Activator.CreateInstance(dbContextType, new object[] { dbOptionBuilder.Options }) as T;
            }
            else
            {
                dbContext = Activator.CreateInstance(dbContextType, new object[] { dbOptionBuilder.Options, serviceProvider }) as T;
            }

            if (dbContext == null)
            {
                throw new NcfDatabaseException($"未能创建 {dbContextType.FullName} 的实例", DatabaseConfigurationFactory.Instance.Current.GetType(), null);
            }
            return dbContext;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <param name="serviceProvider">ServiceProvider</param>
        /// <returns></returns>
        public DbContext GetXncfDbContext(Type xncfDatabaseRegisterType, string connectionString = null, XncfDatabaseData xncfDatabaseData = null,
            Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null, IServiceProvider serviceProvider = null)
        {
            if (!typeof(IXncfDatabase).IsAssignableFrom(xncfDatabaseRegisterType))
            {
                throw new NcfDatabaseException($"{nameof(xncfDatabaseRegisterType)} 参数：{xncfDatabaseRegisterType.Name} 必须实现 IXncfDatabase 接口", DatabaseConfigurationFactory.Instance.Current.GetType());
            }

            //Legacy comment translated to English.
            var dbContextType = GetXncfDbContextType(xncfDatabaseRegisterType);

            return this.GetType().GetMethod(nameof(GetDbContext))
                .MakeGenericMethod(new Type[] { dbContextType })
                .Invoke(this, new object[] { connectionString, xncfDatabaseData, dbContextOptionsAction, serviceProvider }) as DbContext;

            //return GetDbContext(dbContextType, connectionString, xncfDatabaseData, dbContextOptionsAction, serviceProvider);
        }
    }
}
