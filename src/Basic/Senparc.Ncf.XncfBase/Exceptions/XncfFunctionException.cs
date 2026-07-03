/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfFunctionException.cs
    文件功能描述：XncfFunctionException 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Xncf 模块方法执行异常
    /// </summary>
    public class XncfFunctionException : NcfExceptionBase
    {
        public XncfFunctionException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        public XncfFunctionException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
