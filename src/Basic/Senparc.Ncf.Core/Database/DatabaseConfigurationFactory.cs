using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Database.SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Database
{
    public class DatabaseConfigurationFactory
    {
        #region 单例

        DatabaseConfigurationFactory() { }

        /// <summary>
        /// DatabaseConfigurationFactory 的全局单例
        /// </summary>
        public static DatabaseConfigurationFactory Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        class Nested
        {
            static Nested() { }

            internal static readonly DatabaseConfigurationFactory instance = new DatabaseConfigurationFactory();
        }

        #endregion

        //TODO:如果是分布式，需要存储到缓存中

        private IDatabaseConfiguration _currentDatabaseConfiguration;

        public IDatabaseConfiguration CurrentDatabaseConfiguration
        {
            get
            {
                if (_currentDatabaseConfiguration == null)
                {
                    //如果未配置，则默认使用 SQLiteDatabaseConfiguration 内存数据库
                    _currentDatabaseConfiguration = new SQLiteMemoryDatabaseConfiguration();
                }
                return _currentDatabaseConfiguration;
            }
            set
            {
                _currentDatabaseConfiguration = value;
            }
        }
    }
}
