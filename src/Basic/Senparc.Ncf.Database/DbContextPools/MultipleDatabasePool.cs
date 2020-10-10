using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Database;
using Senparc.Ncf.Database.MultipleMigrationDbContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Database
{
    /// <summary>
    /// 多数据库配置池
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
        /// 添加配置
        /// </summary>
        /// <param name="multiDbContextAttr"></param>
        /// <param name="xncfDatabaseDbContextType">实现了 IXncfDatabase 接口的类型</param>
        /// <returns></returns>
        public string TryAdd(MultipleMigrationDbContextAttribute multiDbContextAttr, Type xncfDatabaseDbContextType)
        {
            var msg = $"[{SystemTime.Now}] 检测到数据库 DbContext：{multiDbContextAttr.MultipleDatabaseType} - {multiDbContextAttr.XncfDatabaseRegisterType.Name}";

            //查看是否已经包含 MultipleDatabaseType 
            if (!this.ContainsKey(multiDbContextAttr.MultipleDatabaseType))
            {
                //添加 MultipleDatabaseType 对应集合
                this[multiDbContextAttr.MultipleDatabaseType] = new Dictionary<Type, Type>();
            }

            //加入配置
            this[multiDbContextAttr.MultipleDatabaseType][xncfDatabaseDbContextType] = multiDbContextAttr.XncfDatabaseRegisterType;

            //暂时休眠  -A7B5C6
            ////同步添加到 XncfDatabaseDbContextPool
            //XncfDatabaseDbContextPool.Instance.TryAdd(multiDbContextAttr, xncfDatabaseDbContextType);

            return msg;
        }

        /// <summary>
        /// 获取指定 IXncfDatabase 关联的当前数据库上下文（DbContext）
        /// </summary>
        /// <param name="xncfDatabaseRegisterType">实现了 IXncfDatabase 的具体类型</param>
        /// <returns></returns>
        public Type GetXncfDbContextType(Type xncfDatabaseRegisterType)
        {
            var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
            var currentDatabaseConfiguration = databaseConfigurationFactory.CurrentDatabaseConfiguration;
            MultipleDatabaseType multipleDatabaseType = currentDatabaseConfiguration.MultipleDatabaseType;
            if (!this.ContainsKey(multipleDatabaseType))
            {
                throw new NcfDatabaseException($"未发现已注册的未支持数据库：{multipleDatabaseType}", currentDatabaseConfiguration.GetType());
            }

            var xncdDatabaseRegisterCollection = this[multipleDatabaseType];
            if (!xncdDatabaseRegisterCollection.ContainsKey(xncfDatabaseRegisterType))
            {
                throw new NcfDatabaseException($"{xncfDatabaseRegisterType.Name} 模块未支持数据库：{multipleDatabaseType}", currentDatabaseConfiguration.GetType());
            }

            return xncdDatabaseRegisterCollection[xncfDatabaseRegisterType];
        }

    }
}
