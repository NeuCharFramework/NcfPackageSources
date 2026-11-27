/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfFunctionException.cs
    File description: XncfFunctionException related implementation

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
    /// <summary>
    /// Represents an exception raised while discovering, configuring, or invoking an XNCF Function.
    /// </summary>
    public class XncfFunctionException : NcfExceptionBase
    {
        /// <summary>Initializes an XNCF Function exception.</summary>
        /// <param name="message">The diagnostic message.</param>
        /// <param name="logged">Whether the base exception should be treated as already logged.</param>
        public XncfFunctionException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        /// <summary>Initializes an XNCF Function exception with an underlying cause.</summary>
        /// <param name="message">The diagnostic message.</param>
        /// <param name="inner">The underlying exception.</param>
        /// <param name="logged">Whether the base exception should be treated as already logged.</param>
        public XncfFunctionException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
