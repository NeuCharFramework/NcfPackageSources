/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfUninstallException.cs
    File description: NcfUninstallException related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{
    /// <summary>Indicates that an XNCF module uninstall operation failed.</summary>
    public class NcfUninstallException : NcfExceptionBase
    {
        /// <summary>Initializes an uninstall exception with an optional logging flag.</summary>
        /// <param name="message">The failure message.</param>
        /// <param name="logged">Whether the failure has already been logged.</param>
        public NcfUninstallException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        /// <summary>Initializes an uninstall exception with an underlying exception.</summary>
        /// <param name="message">The failure message.</param>
        /// <param name="inner">The underlying exception.</param>
        /// <param name="logged">Whether the failure has already been logged.</param>
        public NcfUninstallException(string message, Exception inner = null, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
