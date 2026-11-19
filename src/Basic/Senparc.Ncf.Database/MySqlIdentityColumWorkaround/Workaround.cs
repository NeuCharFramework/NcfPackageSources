/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Workaround.cs
    File description: Workaround related implementation

    Created by: Senparc - 20210615

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class MySqlHelper
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="enumName"></param>
        /// <returns></returns>
        public static object GetMySqlValueGenerationStrategy(string enumName = "IdentityColumn")
        {
            var field = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.MySqlValueGenerationStrategy,Pomelo.EntityFrameworkCore.MySql").GetFields().FirstOrDefault(z => z.Name == enumName);
            if (field == null)
            {
                //Legacy comment translated to English.
                var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
                //currentdatabaseconfiguration
                var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
                //Legacy comment translated to English.
                throw new Senparc.Ncf.Core.Exceptions.NcfDatabaseException("程序集中未找到 MySqlValueGenerationStrategy.IdentityColumn 枚举", currentDatabaseConfiguration.GetType());
            }
            var value = field.GetValue(null);
            return value;
        }
    }
}