/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MultipleDatabasePool.cs
    文件功能描述：MultipleDatabasePool 相关实现
    
    
    创建标识：Senparc - 20201010
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260817
    修改描述：v0.21.8-preview8 多库扫描日志改为 Register×Database 矩阵，并单独输出当前 DbContext 绑定

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
        /// <param name="migrationDbContextType">设计时（或运行时进行 Database.Migrate() 操作的）所使用的 XncfDatabaseDbContext 类型</param>
        /// <param name="runtimeDbContextType">运行时所使用的 XncfDatabaseDbContext 类型（通常为进行查询时）</param>
        public XncfDatabaseDbContextWapper(Type migrationDbContextType, Type runtimeDbContextType)
        {
            MigrationDbContextType = migrationDbContextType;
            RuntimeDbContextType = runtimeDbContextType;
        }

        public Type MigrationDbContextType { get; set; }
        public Type RuntimeDbContextType { get; set; }
    }

    /// <summary>
    /// 多数据库配置池
    /// <para>Value 为 Dictionary<Type/* IXncfDatabase Register 类型*/, Type/* 数据库 XncfDatabaseDbContext 类型 */></para>
    /// </summary>
    public class MultipleDatabasePool
        : Dictionary<MultipleDatabaseType, Dictionary<Type/* IXncfDatabase Register 类型*/, Type/* 数据库 XncfDatabaseDbContext 类型 */>>
    {
        #region 单例

        MultipleDatabasePool() { }

        /// <summary>
        /// DatabaseConfigurationFactory 的全局单例
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
        /// 单元测试用的 DbContext
        /// </summary>
        public static Type UnitTestPillarDbContext { get; set; } = null;

        /// <summary>
        /// 添加配置（扫描阶段仅登记，日志请在扫描结束后调用 <see cref="BuildSupportMatrixLog"/>）
        /// </summary>
        /// <param name="multiDbContextAttr"></param>
        /// <param name="xncfDatabaseDbContextType">实现了多数据库 DbContext 的类型</param>
        public void TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType)
        {
            //查看是否已经包含 MultipleDatabaseType 
            if (!this.ContainsKey(multiDbContextAttr.MultipleDatabaseType))
            {
                //添加 MultipleDatabaseType 对应集合
                this[multiDbContextAttr.MultipleDatabaseType] = new Dictionary<Type, Type>();
            }

            //加入配置
            this[multiDbContextAttr.MultipleDatabaseType][multiDbContextAttr.XncfDatabaseRegisterType] = xncfDatabaseDbContextType;

            //同步添加到 XncfDatabaseDbContextPool
            XncfDatabaseDbContextPool.Instance.TryAdd(multiDbContextAttr, xncfDatabaseDbContextType);
        }

        /// <summary>
        /// 生成多数据库支持矩阵日志：行为 Register，列为数据库类型；并单独列出当前库的 DbContext 绑定。
        /// </summary>
        /// <param name="currentDatabaseType">当前正在使用的数据库（可为空；为空时不标记 * / 不输出当前绑定表）</param>
        /// <returns>多行文本日志（不含时间戳前缀）</returns>
        public string BuildSupportMatrixLog(MultipleDatabaseType? currentDatabaseType = null)
        {
            return BuildSupportMatrixLog(XncfDatabaseDbContextPool.Instance, currentDatabaseType);
        }

        /// <summary>
        /// 生成多数据库支持矩阵日志（可注入 map，便于单测）
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

            // 列：已出现的库类型 + 当前配置库（若有）；排除 Other
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

            // 当前库 DbContext 明细
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
        /// 获取指定 IXncfDatabase 关联的当前数据库上下文（DbContext）
        /// </summary>
        /// <param name="xncfDatabaseRegister">实现了 IXncfDatabase 的实体</param>
        /// <returns></returns>
        public Type GetXncfDbContextType(IXncfDatabase xncfDatabaseRegister)
        {
            return GetXncfDbContextType(xncfDatabaseRegister.GetType());
        }

        /// <summary>
        /// 获取指定 IXncfDatabase 关联的当前数据库上下文（DbContext）
        /// </summary>
        /// <param name="xncfDatabaseRegisterType">实现了 IXncfDatabase 的具体类型</param>
        /// <returns></returns>
        public Type GetXncfDbContextType(Type xncfDatabaseRegisterType)
        {
            //数据库配置工厂
            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            //当前数据库配置
            var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
            //当前数据库类型
            MultipleDatabaseType multipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;

            if (multipleDatabaseType == MultipleDatabaseType.InMemory)
            {
                //单元测试
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
        /// 获取指定 DbContext 的数据库实例
        /// </summary>
        /// <param name="connectionString">连接字符串，如果为 null，则默认使用 SenparcDatabaseConfigs.ClientConnectionString</param>
        /// <param name="dbContextOptionsAction">额外配置操作</param>
        /// <param name="xncfDatabaseData">IXncfDatabase 信息（仅在针对 XNCF 进行数据库迁移时有效）</param>
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
                //带泛型
                //准备创建 DbContextOptionsBuilder 实例，定义类型
                dbOptionBuilderType = typeof(DbContextOptionsBuilder<>);
                //dbOptionBuilderType = typeof(RelationalDbContextOptionsBuilder<,>);
                //获取泛型对象类型，如：DbContextOptionsBuilder<SenparcEntities>
                dbOptionBuilderType = dbOptionBuilderType.MakeGenericType(dbContextType);

                //创建 DbContextOptionsBuilder 实例
                dbOptionBuilder = Activator.CreateInstance(dbOptionBuilderType) as DbContextOptionsBuilder;
            }
            else
            {
                //不带泛型
                dbOptionBuilder = new DbContextOptionsBuilder();
            }

            //if (UnitTestDatabaseConfiguration.UnitTestPillarDbContext == null)
            {
                //不是单元测试，需要读取数据库

                //获取当前数据库配置
                var currentDatabasConfiguration = DatabaseConfigurationFactory.Instance.Current;
                //指定使用当前数据库
                currentDatabasConfiguration.UseDatabase(
                    dbOptionBuilder,
                    connectionString ?? (SenparcDatabaseConnectionConfigs.GetClientConnectionString()),
                    xncfDatabaseData,
                    dbContextOptionsAction
                    );
            }

            //实例化 DbContext
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
        /// 获取指定 xncfDatabaseRegister 关联的当前数据库实例
        /// </summary>
        /// <param name="xncfDatabaseRegisterType">实现了 IXncfDatabase 的具体类型</param>
        /// <param name="connectionString">连接字符串，如果为 null，则默认使用 SenparcDatabaseConfigs.ClientConnectionString</param>
        /// <param name="dbContextOptionsAction">额外配置操作</param>
        /// <param name="xncfDatabaseData">IXncfDatabase 信息（仅在针对 XNCF 进行数据库迁移时有效）</param>
        /// <param name="serviceProvider">ServiceProvider</param>
        /// <returns></returns>
        public DbContext GetXncfDbContext(Type xncfDatabaseRegisterType, string connectionString = null, XncfDatabaseData xncfDatabaseData = null,
            Action<IRelationalDbContextOptionsBuilderInfrastructure, XncfDatabaseData> dbContextOptionsAction = null, IServiceProvider serviceProvider = null)
        {
            if (!typeof(IXncfDatabase).IsAssignableFrom(xncfDatabaseRegisterType))
            {
                throw new NcfDatabaseException($"{nameof(xncfDatabaseRegisterType)} 参数：{xncfDatabaseRegisterType.Name} 必须实现 IXncfDatabase 接口", DatabaseConfigurationFactory.Instance.Current.GetType());
            }

            //获取 DbContext 上下文类型
            var dbContextType = GetXncfDbContextType(xncfDatabaseRegisterType);

            return this.GetType().GetMethod(nameof(GetDbContext))
                .MakeGenericMethod(new Type[] { dbContextType })
                .Invoke(this, new object[] { connectionString, xncfDatabaseData, dbContextOptionsAction, serviceProvider }) as DbContext;

            //return GetDbContext(dbContextType, connectionString, xncfDatabaseData, dbContextOptionsAction, serviceProvider);
        }
    }
}
