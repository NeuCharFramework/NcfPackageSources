using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    public class XncfPageException : NcfExceptionBase
    {
        public IXncfRegister XncfRegister { get; set; }

        public XncfPageException(IXncfRegister xncfRegister, string message, bool logged = false) : base(message, logged)
        {
            XncfRegister = xncfRegister;
        }

        public XncfPageException(IXncfRegister xncfRegister, string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
            XncfRegister = xncfRegister;
        }
    }
}
