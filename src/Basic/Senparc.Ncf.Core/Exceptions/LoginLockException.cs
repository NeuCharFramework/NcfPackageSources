/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：LoginLockException.cs
    文件功能描述：LoginLockException 相关实现
    
    
    创建标识：Senparc - 20250111
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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