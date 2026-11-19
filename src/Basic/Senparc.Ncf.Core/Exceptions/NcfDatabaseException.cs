/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfDatabaseException.cs
    File description: NcfDatabaseException related implementation

    Created by: Senparc - 20201001

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class NcfDatabaseException : NcfExceptionBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="typeOfDatabaseConfiguration">DatabaseConfiguration type</param>
        ///Legacy comment translated to English.
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
