/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfTenantException.cs
    文件功能描述：NcfTenantException 相关实现
    
    
    创建标识：Senparc - 20210213
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NcfTenantException : NcfExceptionBase
    {
        public NcfTenantException(string message, bool logged = false) : base(message, logged)
        {
        }

        public NcfTenantException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
