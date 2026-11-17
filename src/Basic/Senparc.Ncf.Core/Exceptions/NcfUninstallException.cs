/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfUninstallException.cs
    File description: NcfUninstallException related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class NcfUninstallException : NcfExceptionBase
    {
        public NcfUninstallException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        public NcfUninstallException(string message, Exception inner = null, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
