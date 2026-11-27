/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IXncfThread.cs
    File description: IXncfThread related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Defines the optional background-thread registration contract for an XNCF module.
    /// </summary>
    public interface IXncfThread
    {
        /// <summary>
        /// Adds the module's background tasks to the shared XNCF thread builder.
        /// </summary>
        /// <param name="xncfThreadBuilder">The builder receiving the module thread definitions.</param>
        void ThreadConfig(XncfThreadBuilder xncfThreadBuilder);
    }
}
