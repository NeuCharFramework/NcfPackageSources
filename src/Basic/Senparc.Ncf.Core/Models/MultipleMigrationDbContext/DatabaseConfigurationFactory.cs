using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 多数据库配置工厂
    /// </summary>
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

        public IDatabaseConfiguration Current
        {
            get
            {
                if (_currentDatabaseConfiguration == null)
                {
                    throw new NcfDatabaseException("未指定 DatabaseConfiguration！", null);
                }
                return _currentDatabaseConfiguration;
            }
            set
            {
                _currentDatabaseConfiguration = value;
            }
        }


        ///// <summary>
        ///// 给 design time（设计时）操作数据库（如migration）使用。指定当前正在操作的 XNCF 数据库信息（如果是直接继承自 DbContext 的类，需要模拟此参数）
        ///// </summary>
        //public XncfDatabaseData CurrentXncfDatabaseData { get; set; }
    }
}
