using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Exceptions
{
    public class NCFExceptionBase : CO2NET.Exceptions.BaseException
    {
        public NCFExceptionBase(string message, bool logged = false) : base(message, logged)
        {
        }

        public NCFExceptionBase(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}
