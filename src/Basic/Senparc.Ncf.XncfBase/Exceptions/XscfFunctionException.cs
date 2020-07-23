using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Xscf 模块方法执行异常
    /// </summary>
    public class XscfFunctionException : SCFExceptionBase
    {
        public XscfFunctionException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        public XscfFunctionException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
