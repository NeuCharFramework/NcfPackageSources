/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfModuleException.cs
    File description: NcfModuleException related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    public class XncfPageException : NcfExceptionBase
    {
        public IXncfRegister XncfRegister { get; set; }

        /// <summary>
        /// Initializes a page exception associated with an XNCF registration.
        /// </summary>
        /// <param name="xncfRegister">The affected module registration.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="logged">Whether the exception has already been logged.</param>
        public XncfPageException(IXncfRegister xncfRegister, string message, bool logged = false) : base(message, logged)
        {
            XncfRegister = xncfRegister;
        }

        /// <summary>
        /// Initializes a page exception with an associated module and inner exception.
        /// </summary>
        /// <param name="xncfRegister">The affected module registration.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The underlying exception.</param>
        /// <param name="logged">Whether the exception has already been logged.</param>
        public XncfPageException(IXncfRegister xncfRegister, string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
            XncfRegister = xncfRegister;
        }
    }
}
