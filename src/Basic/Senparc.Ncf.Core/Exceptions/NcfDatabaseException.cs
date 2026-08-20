/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfDatabaseException.cs
    文件功能描述：NcfDatabaseException 相关实现
    
    
    创建标识：Senparc - 20201001
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    /// <summary>
    /// NCF 数据库异常
    /// </summary>
    public class NcfDatabaseException : NcfExceptionBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="typeOfDatabaseConfiguration">DatabaseConfiguration 类型</param>
        /// <param name="typeOfDbContext">DbContext 类型，如：SenparcEntities</param>
        /// <param name="inner"></param>
        public NcfDatabaseException(string message, Type typeOfDatabaseConfiguration, Type typeOfDbContext = null, Exception inner = null)
            : base(message, inner, true)
        {
            message += @$"
DatabaseConfiguration 类型：{(typeOfDatabaseConfiguration == null ? "未提供" : typeOfDatabaseConfiguration.Name)}
DbContext 类型：{(typeOfDbContext == null ? "未提供" : typeOfDbContext.Name)}
";

            SenparcTrace.BaseExceptionLog(this);
        }

    }
}
