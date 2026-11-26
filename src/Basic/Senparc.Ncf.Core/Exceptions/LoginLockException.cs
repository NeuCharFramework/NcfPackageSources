/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LoginLockException.cs
    File description: LoginLockException related implementation

    Created by: Senparc - 20250111

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Exceptions
{
    /// <summary>
    /// Indicates that an account is temporarily locked and exposes the next retry time.
    /// </summary>
    public class LoginLockException : NcfExceptionBase
    {
        public DateTime LockEndTime { get; private set; }

        /// <summary>
        /// Initializes a login-lock exception with its retry time.
        /// </summary>
        /// <param name="lockEndTime">The time at which another login may be attempted.</param>
        /// <param name="showWrongLoginMessage">Whether the message should include a credential-error hint.</param>
        public LoginLockException(DateTime lockEndTime, bool showWrongLoginMessage=false)
            : base($"{(showWrongLoginMessage?"账号或密码错误！":"")}账号已被锁定，请在 {lockEndTime:yyyy-MM-dd HH:mm:ss} 后重试")
        {
            LockEndTime = lockEndTime;
        }
    }
}
