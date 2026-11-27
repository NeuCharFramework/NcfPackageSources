/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentsManagerException.cs
    File description: AgentsManagerException related implementation

    Created by: Senparc - 20250531

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.0.0 Add admin chat-tool task orchestration and MCP-aware chat improvements.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Exceptions
{
    public class AgentsManagerException : NcfExceptionBase
    {
        /// <summary>
        /// Initializes an AgentsManager exception with an optional logging flag.
        /// </summary>
        /// <param name="message">The user-facing or diagnostic message.</param>
        /// <param name="logged">Whether the exception has already been logged.</param>
        public AgentsManagerException(string message, bool logged = false) : base(message, logged)
        {
        }

        /// <summary>
        /// Initializes an AgentsManager exception with an inner exception and optional logging flag.
        /// </summary>
        /// <param name="message">The normalized exception message.</param>
        /// <param name="inner">The underlying exception.</param>
        /// <param name="logged">Whether the exception has already been logged.</param>
        public AgentsManagerException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
