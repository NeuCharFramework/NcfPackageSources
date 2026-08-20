/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfModuleException.cs
    文件功能描述：NcfModuleException 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NcfModuleException : NcfExceptionBase
    {
        //public IXncfRegister XncfRegister;
        public NcfModuleException(string message, bool logged = false) : base(message, logged)
        {
        }

        public NcfModuleException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
