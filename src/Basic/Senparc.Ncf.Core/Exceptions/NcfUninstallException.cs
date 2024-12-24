using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{
    /// <summary>
    /// NCF 未安装
    /// </summary>
    public class NcfUninstallException : NcfExceptionBase
    {
        public NcfUninstallException(string message, bool logged = false) : this(message, null, logged)
        {
        }

        public NcfUninstallException(string message, Exception inner = null, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
