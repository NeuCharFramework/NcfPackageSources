using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Multi-database configuration factory
    /// </summary>
    public class DatabaseConfigurationFactory
    {
        #region Singleton

        DatabaseConfigurationFactory() { }

        /// <summary>
        ///Global singleton of DatabaseConfigurationFactory
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

        //TODO: If it is distributed, it needs to be stored in the cache.

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
        ///// Used for design time (design time) operation database (such as migration). Specify the XNCF database information currently being operated (if it is a class directly inherited from DbContext, this parameter needs to be simulated)
        ///// </summary>
        //public XncfDatabaseData CurrentXncfDatabaseData { get; set; }
    }
}
