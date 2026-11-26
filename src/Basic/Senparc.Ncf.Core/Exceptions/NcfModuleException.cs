/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfModuleException.cs
    File description: NcfModuleException related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NcfModuleException : NcfExceptionBase
    {
        //public IXncfRegister XncfRegister;
        /// <summary>
        /// Initializes a module-scoped exception with an optional logging flag.
        /// </summary>
        /// <param name="message">The module failure message.</param>
        /// <param name="logged">Whether the exception has already been logged.</param>
        public NcfModuleException(string message, bool logged = false) : base(message, logged)
        {
        }

        /// <summary>
        /// Initializes a module-scoped exception with an inner failure and optional logging flag.
        /// </summary>
        /// <param name="message">The module failure message.</param>
        /// <param name="inner">The underlying exception.</param>
        /// <param name="logged">Whether the exception has already been logged.</param>
        public NcfModuleException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
