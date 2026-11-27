/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Workaround.cs
    File description: Workaround related implementation

    Created by: Senparc - 20210615

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.14 Updated database integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.21.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    /// Provides a reflection-based compatibility lookup for the Pomelo MySQL
    /// identity-column strategy enum.
    /// </summary>
    public class MySqlHelper
    {
        /// <summary>
        /// Resolves a MySQL value-generation strategy enum member from the provider assembly.
        /// </summary>
        /// <param name="enumName">The enum member name, defaulting to <c>IdentityColumn</c>.</param>
        /// <returns>The reflected enum value.</returns>
        /// <exception cref="Senparc.Ncf.Core.Exceptions.NcfDatabaseException">Thrown when the provider enum member cannot be found.</exception>
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
