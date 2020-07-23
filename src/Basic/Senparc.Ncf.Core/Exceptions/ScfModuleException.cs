using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class ScfModuleException : SCFExceptionBase
    {
        //public IXscfRegister XscfRegister;
        public ScfModuleException(string message, bool logged = false) : base(message, logged)
        {
        }

        public ScfModuleException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
