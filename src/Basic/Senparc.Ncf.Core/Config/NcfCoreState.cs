using Senparc.Ncf.Utility.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Config
{
    /// <summary>
    /// NCF system state
    /// </summary>
    public record class NcfCoreState
    {
        #region Singleton

        NcfCoreState() { }

        /// <summary>
        /// Global singleton for DatabaseConfigurationFactory
        /// </summary>
        public static NcfCoreState Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        class Nested
        {
            static Nested() { }

            internal static readonly NcfCoreState instance = new NcfCoreState();
        }

        #endregion

        /// <summary>
        /// Indicates all AddXncfModule operations have completed
        /// </summary>
        public bool AllAddXncfModuleApplied { get; set; }
        /// <summary>
        /// Indicates all UseXncfModuleApplied operations have completed
        /// </summary>
        public bool AllUseXncfModuleApplied { get; set; }
        /// <summary>
        /// Indicates all AuthorizeConfig operations are complete, including Area permission setup and IAreaRegister execution
        /// </summary>
        public bool AllAuthorizeConfigApplied { get; set; }

        /// <summary>
        /// Indicates database registration for any XNCF module has been loaded
        /// </summary>
        public bool AynDatabaseXncfLoaded { get; set; }
        /// <summary>
        /// Indicates database registrations for all XNCF modules have been loaded
        /// </summary>
        public bool AllDatabaseXncfLoaded { get; set; }

        /// <summary>
        /// Indicates thread registration for any XNCF module has been loaded
        /// </summary>
        public bool AnyThreadXncfLoaded { get; set; }
        /// <summary>
        /// Indicates thread registrations for all XNCF modules have been loaded
        /// </summary>
        public bool AllThreadXncfLoaded { get; set; }

        /// <summary>
        /// Indicates AddXncfDatabaseModule has been loaded for at least one XNCF module
        /// </summary>
        public bool AnyAddXncfDatabaseModuleApplied { get; set; }

        /// <summary>
        /// Included DLL file-name patterns. ".Xncf." is always included.
        /// </summary>
        public List<string> DllFilePatterns { get; set; }

        public SystemLanguage SystemLanguage => GlobalCulture.CurrentLanguage;
    }
}
