/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Workaround.cs
    文件功能描述：Workaround 相关实现
    
    
    创建标识：Senparc - 20210615
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    /// 弥补 MySQL 库暂时的缺陷
    /// </summary>
    public class MySqlHelper
    {
        /// <summary>
        /// 获取 MySqlValueGenerationStrateg.IdentityColumn 等枚举
        /// </summary>
        /// <param name="enumName"></param>
        /// <returns></returns>
        public static object GetMySqlValueGenerationStrategy(string enumName = "IdentityColumn")
        {
            var field = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.MySqlValueGenerationStrategy,Pomelo.EntityFrameworkCore.MySql").GetFields().FirstOrDefault(z => z.Name == enumName);
            if (field == null)
            {
                //数据库配置工厂
                var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
                //当前数据库配置
                var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
                //抛出异常
                throw new Senparc.Ncf.Core.Exceptions.NcfDatabaseException("程序集中未找到 MySqlValueGenerationStrategy.IdentityColumn 枚举", currentDatabaseConfiguration.GetType());
            }
            var value = field.GetValue(null);
            return value;
        }
    }
}