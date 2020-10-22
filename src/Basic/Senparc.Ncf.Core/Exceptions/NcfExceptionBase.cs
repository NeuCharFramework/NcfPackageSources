using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NcfExceptionBase : CO2NET.Exceptions.BaseException
    {
        public NcfExceptionBase(string message, bool logged = false) : base(message, logged)
        {
        }

        public NcfExceptionBase(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
