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
