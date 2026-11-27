/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfDatabaseException.cs
    File description: NcfDatabaseException related implementation

    Created by: Senparc - 20201001

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    /// <summary>
    /// Represents a database configuration, provider, or DbContext failure in NCF.
    /// </summary>
    public class NcfDatabaseException : NcfExceptionBase
    {
        /// <summary>
        /// Initializes a database exception and records the related provider/context types.
        /// </summary>
        /// <param name="message">The primary failure message.</param>
        /// <param name="typeOfDatabaseConfiguration">The database configuration type involved.</param>
        /// <param name="typeOfDbContext">The DbContext type involved, when known.</param>
        /// <param name="inner">The underlying exception, when available.</param>
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
