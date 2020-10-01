using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NCFeExceptionBase : CO2NET.Exceptions.BaseException
    {
        public NCFeExceptionBase(string message, bool logged = false) : base(message, logged)
        {
        }

        public NCFeExceptionBase(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
