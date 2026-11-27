/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseConfigurationFactory.cs
    File description: DatabaseConfigurationFactory related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Stores the active database-provider configuration used by NCF.
    /// </summary>
    public class DatabaseConfigurationFactory
    {
        #region Legacy section

        DatabaseConfigurationFactory() { }

        /// <summary>
        /// Gets the singleton database configuration factory.
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

        private IDatabaseConfiguration _currentDatabaseConfiguration;

        /// <summary>
        /// Gets or sets the active database configuration.
        /// </summary>
        /// <exception cref="NcfDatabaseException">Thrown when the getter is used before a configuration is assigned.</exception>
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


        // XncfDatabaseData is now passed explicitly through provider callbacks
        // instead of being stored as mutable global factory state.
    }
}
