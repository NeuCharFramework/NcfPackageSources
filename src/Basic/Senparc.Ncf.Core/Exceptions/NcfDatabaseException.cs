using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    /// <summary>
    /// NCF database exception
    /// </summary>
    public class NcfDatabaseException : NcfExceptionBase
    {
        /// <summary>
        ///Constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="typeOfDatabaseConfiguration">DatabaseConfiguration type</param>
        /// <param name="typeOfDbContext">DbContext type, such as: SenparcEntities</param>
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
