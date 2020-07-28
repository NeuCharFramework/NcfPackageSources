using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Xncf 模块方法执行异常
    /// </summary>
    public class XncfFunctionException : NCFExceptionBase
    {
        public XncfFunctionException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        public XncfFunctionException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
