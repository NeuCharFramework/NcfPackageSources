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
