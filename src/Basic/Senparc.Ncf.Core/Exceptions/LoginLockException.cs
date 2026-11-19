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
    public class LoginLockException : NcfExceptionBase
    {
        public DateTime LockEndTime { get; private set; }

        public LoginLockException(DateTime lockEndTime, bool showWrongLoginMessage=false)
            : base($"{(showWrongLoginMessage?"账号或密码错误！":"")}账号已被锁定，请在 {lockEndTime:yyyy-MM-dd HH:mm:ss} 后重试")
        {
            LockEndTime = lockEndTime;
        }
    }
}