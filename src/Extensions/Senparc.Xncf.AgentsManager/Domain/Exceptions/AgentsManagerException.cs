/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AgentsManagerException.cs
    文件功能描述：AgentsManagerException 相关实现
    
    
    创建标识：Senparc - 20250531
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        public AgentsManagerException(string message, bool logged = false) : base(message, logged)
        {
        }

        public AgentsManagerException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
