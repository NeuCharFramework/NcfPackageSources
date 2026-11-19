/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfDatabaseUpgradeRequiredException.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.28.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Exceptions
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class NcfDatabaseUpgradeRequiredException : NcfExceptionBase
    {
        public NcfDatabaseUpgradeRequiredException(string message, Exception inner = null)
            : base(message, inner)
        {
        }
    }
}
