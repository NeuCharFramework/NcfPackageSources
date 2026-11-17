/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfCoreState.cs
    File description: NcfCoreState related implementation

    Created by: Senparc - 20240331

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Utility.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Config
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public record class NcfCoreState
    {
        #region Legacy section

        NcfCoreState() { }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public bool AllAddXncfModuleApplied { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AllUseXncfModuleApplied { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AllAuthorizeConfigApplied { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AynDatabaseXncfLoaded { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AllDatabaseXncfLoaded { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AnyThreadXncfLoaded { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AllThreadXncfLoaded { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AnyAddXncfDatabaseModuleApplied { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<string> DllFilePatterns { get; set; }

        public SystemLanguage SystemLanguage => GlobalCulture.CurrentLanguage;
    }
}
