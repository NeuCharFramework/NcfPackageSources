using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    /// Make up for the temporary shortcomings of the MySQL library
    /// </summary>
    public class MySqlHelper
    {
        /// <summary>
        /// Get enumerations such as MySqlValueGenerationStrateg.IdentityColumn
        /// </summary>
        /// <param name="enumName"></param>
        /// <returns></returns>
        public static object GetMySqlValueGenerationStrategy(string enumName = "IdentityColumn")
        {
            var field = Type.GetType("Microsoft.EntityFrameworkCore.Metadata.MySqlValueGenerationStrategy,Pomelo.EntityFrameworkCore.MySql").GetFields().FirstOrDefault(z => z.Name == enumName);
            if (field == null)
            {
                //Database configuration factory
                var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
                //Current database configuration
                var currentDatabaseConfiguration = databaseConfigurationFactory.Current;
                //throw an exception
                throw new Senparc.Ncf.Core.Exceptions.NcfDatabaseException("程序集中未找到 MySqlValueGenerationStrategy.IdentityColumn 枚举", currentDatabaseConfiguration.GetType());
            }
            var value = field.GetValue(null);
            return value;
        }
    }
}