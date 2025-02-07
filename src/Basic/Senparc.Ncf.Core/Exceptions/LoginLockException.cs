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