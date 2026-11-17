/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseConfigurationFactory.cs
    File description: DatabaseConfigurationFactory related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class DatabaseConfigurationFactory
    {
        #region Legacy section

        DatabaseConfigurationFactory() { }

        /// <summary>
        ///Legacy comment translated to English.
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

        //Legacy comment translated to English.

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
        ///Legacy comment translated to English.
        ///// </summary>
        //public XncfDatabaseData CurrentXncfDatabaseData { get; set; }
    }
}
