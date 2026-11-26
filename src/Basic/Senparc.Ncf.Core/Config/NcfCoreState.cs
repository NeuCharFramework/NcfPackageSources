/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfCoreState.cs
    File description: NcfCoreState related implementation

    Created by: Senparc - 20240331

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Utility.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Config
{
    /// <summary>
    /// Stores process-wide NCF startup and module-registration state.
    /// </summary>
    public record class NcfCoreState
    {
        #region Legacy section

        NcfCoreState() { }

        /// <summary>
        /// Gets the singleton startup-state instance.
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
        /// Gets or sets whether all module service registrations have been applied.
        /// </summary>
        public bool AllAddXncfModuleApplied { get; set; }
        /// <summary>
        /// Gets or sets whether all module middleware registrations have been applied.
        /// </summary>
        public bool AllUseXncfModuleApplied { get; set; }
        /// <summary>
        /// Gets or sets whether all area authorization configurations have been applied.
        /// </summary>
        public bool AllAuthorizeConfigApplied { get; set; }

        /// <summary>
        /// Gets or sets whether any database XNCF has been loaded.
        /// </summary>
        public bool AynDatabaseXncfLoaded { get; set; }
        /// <summary>
        /// Gets or sets whether all database XNCFs have been loaded.
        /// </summary>
        public bool AllDatabaseXncfLoaded { get; set; }

        /// <summary>
        /// Gets or sets whether any background thread has been loaded.
        /// </summary>
        public bool AnyThreadXncfLoaded { get; set; }
        /// <summary>
        /// Gets or sets whether all background threads have been loaded.
        /// </summary>
        public bool AllThreadXncfLoaded { get; set; }

        /// <summary>
        /// Gets or sets whether any module database service registration has been applied.
        /// </summary>
        public bool AnyAddXncfDatabaseModuleApplied { get; set; }

        /// <summary>
        /// Gets or sets the assembly-name filters used during module scanning.
        /// </summary>
        public List<string> DllFilePatterns { get; set; }

        public SystemLanguage SystemLanguage => GlobalCulture.CurrentLanguage;
    }
}
